using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Acontplus.Persistence.SqlServer.Exceptions;

/// <summary>
/// Handles SQL Server exceptions by mapping them to domain error types.
/// </summary>
public static class SqlServerExceptionHandler
{
    private static readonly ImmutableDictionary<int, SqlErrorInfo> ErrorMappings = new Dictionary<int, SqlErrorInfo>
    {
        // Timeout errors
        [2] = new(ErrorType.Timeout, "SQL_TIMEOUT", "Database operation timed out"),

        // Service unavailable
        [4060] = new(ErrorType.ServiceUnavailable, "SQL_SERVICE_UNAVAILABLE",
            "Database service is currently unavailable"),
        [40197] = new(ErrorType.ServiceUnavailable, "SQL_SERVICE_BUSY", "Service is busy"),
        [40613] = new(ErrorType.ServiceUnavailable, "SQL_DATABASE_UNAVAILABLE",
            "Database is currently unavailable"),

        // Authentication
        [18456] = new(ErrorType.Unauthorized, "SQL_AUTH_FAILED", "Database authentication failed"),
        [18461] = new(ErrorType.Unauthorized, "SQL_LOGIN_FAILED", "Login failed for user"),

        // Connection
        [53] = new(ErrorType.External, "SQL_CONNECTION_FAILED", "Network path not found"),
        [64] = new(ErrorType.External, "SQL_CONNECTION_FAILED", "Network connection failed"),

        // Constraints
        [2627] = new(ErrorType.Conflict, "SQL_DUPLICATE_KEY", "Duplicate key violation"),
        [2601] = new(ErrorType.Conflict, "SQL_DUPLICATE_INDEX", "Duplicate index violation"),
        [547] = new(ErrorType.Conflict, "SQL_FOREIGN_KEY_VIOLATION", "Foreign key constraint violation"),

        // Validation
        [515] = new(ErrorType.Validation, "SQL_REQUIRED_FIELD_NULL", "Required field cannot be null"),
        [8152] = new(ErrorType.Validation, "SQL_STRING_TOO_LONG", "String or binary data would be truncated"),
        [245] = new(ErrorType.Validation, "SQL_CONVERSION_ERROR", "Conversion failed when converting value"),

        // Deadlock
        [1205] = new(ErrorType.Conflict, "SQL_DEADLOCK", "Transaction was deadlocked"),

        // Permission
        [229] = new(ErrorType.Forbidden, "SQL_PERMISSION_DENIED", "Permission denied"),

        // Resource
        [8645] = new(ErrorType.ServiceUnavailable, "SQL_RESOURCE_UNAVAILABLE",
            "Memory resources temporarily unavailable")
    }.ToImmutableDictionary();

    /// <summary>
    /// Determines whether the exception represents a transient SQL Server error that can be retried.
    /// </summary>
    /// <param name="ex">The SQL exception to evaluate.</param>
    /// <returns><c>true</c> if the error is transient.</returns>
    public static bool IsTransientException(SqlException ex)
    {
        // Special case for transient authentication errors
        return (ex.Number == 18456 && ex.Class == 14) || ErrorRanges.TransientErrors.Contains(ex.Number);
    }

    /// <summary>
    /// Maps a <see cref="SqlException"/> to a <see cref="SqlErrorInfo"/> domain error descriptor.
    /// </summary>
    /// <param name="ex">The SQL exception to map.</param>
    /// <returns>A <see cref="SqlErrorInfo"/> describing the domain error.</returns>
    public static SqlErrorInfo MapSqlException(SqlException ex)
    {
        if (ErrorMappings.TryGetValue(ex.Number, out var errorInfo))
        {
            return errorInfo with { Exception = ex };
        }

        // Handle constraint violations
        if (ex.Number == 547 && ex.Message.Contains("CHECK constraint"))
        {
            return new SqlErrorInfo(
                ErrorType.Validation,
                "SQL_CHECK_CONSTRAINT",
                "Check constraint violation",
                ex);
        }

        // Handle custom stored procedure errors
        return IsCustomStoredProcedureError(ex.Number)
            ? HandleCustomStoredProcedureError(ex)
            : new SqlErrorInfo(
                ErrorType.Internal,
                $"SQL_ERROR_{ex.Number}",
                ex.Message,
                ex);
    }

    private static bool IsCustomStoredProcedureError(int errorNumber)
    {
        // Accept both RAISERROR (13000+) and THROW (50000+) ranges
        return (errorNumber >= ErrorRanges.RaiserrorMin && errorNumber < ErrorRanges.ThrowMin) ||
               (errorNumber >= ErrorRanges.ThrowMin && errorNumber <= ErrorRanges.MaxError);
    }

    private static SqlErrorInfo HandleCustomStoredProcedureError(SqlException ex)
    {
        // Map stored procedure errors to the closest HTTP-aligned ErrorType
        var errorType = ex.Number switch
        {
            // RAISERROR range (13000+)
            >= ErrorRanges.CustomErrors.RaiserrorValidationStart and < ErrorRanges.CustomErrors.RaiserrorBusinessStart
                => ErrorType.Validation,
            >= ErrorRanges.CustomErrors.RaiserrorBusinessStart and < ErrorRanges.ThrowMin
                => ErrorType.Conflict, // Business rules typically map to Conflict (409)

            // THROW range (50000+)
            >= ErrorRanges.CustomErrors.ThrowValidationStart and < ErrorRanges.CustomErrors.ThrowBusinessStart
                => ErrorType.Validation,
            >= ErrorRanges.CustomErrors.ThrowBusinessStart
                => ErrorType.Conflict, // Business rules typically map to Conflict (409)

            _ => ErrorType.Validation
        };

        // Determine prefix based on error class
        var prefix = ex.Class switch
        {
            16 => "SP_BUSINESS_", // Severity 16 indicates business logic errors
            _ => "SP_VALIDATION_"
        };

        return new SqlErrorInfo(
            errorType,
            $"{prefix}{ex.Number}",
            ex.Message,
            ex);
    }

    /// <summary>
    /// Logs and throws a <see cref="SqlException"/> as a <see cref="SqlDomainException"/>.
    /// </summary>
    /// <param name="ex">The SQL exception to handle.</param>
    /// <param name="logger">The logger to write the error details to.</param>
    /// <param name="operation">A description of the operation that failed.</param>
    /// <param name="caller">The name of the calling member (auto-populated).</param>
    public static void HandleSqlException(
        SqlException ex,
        ILogger logger,
        string operation,
        [CallerMemberName] string caller = "")
    {
        var errorInfo = MapSqlException(ex);

        logger.Log(GetLogLevel(errorInfo.ErrorType),
            new EventId(ex.Number, errorInfo.Code),
            "SQL Error in {Operation} called from {Caller}: {ErrorType} - {Message}",
            operation, caller, errorInfo.ErrorType, errorInfo.Message);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("SQL Error Details: {Details}", new
            {
                ex.Number,
                ex.Class,
                ex.State,
                ex.Procedure,
                ex.LineNumber,
                ex.Server,
                ex.ClientConnectionId,
                errorInfo.Code,
                IsTransient = IsTransientException(ex)
            });
        }

        throw new SqlDomainException(errorInfo);
    }

    private static LogLevel GetLogLevel(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation or ErrorType.Conflict or ErrorType.NotFound => LogLevel.Warning,
        ErrorType.Unauthorized or ErrorType.Forbidden => LogLevel.Warning,
        ErrorType.Timeout or ErrorType.ServiceUnavailable => LogLevel.Warning,
        _ => LogLevel.Error
    };

    private static class ErrorRanges
    {
        // SQL Server valid ranges
        public const int RaiserrorMin = 13000; // Minimum for RAISERROR custom errors
        public const int ThrowMin = 50001; // Minimum for THROW custom errors (50000 is reserved)
        public const int MaxError = int.MaxValue; // Maximum for both

        // Standard transient errors
        public static readonly ImmutableHashSet<int> TransientErrors = ImmutableHashSet.Create(
            /* Previous transient error numbers remain the same */
            2, 53, 64, 233, 10053, 10054, 10060, 11001,
            4060, 40197, 40501, 40613, 40143, 40149, 40544, 40549,
            49918, 49919, 49920, 8645, 8651, 1205
        );

        // Error classification ranges
        public static class CustomErrors
        {
            // For RAISERROR (13000+)
            public const int RaiserrorValidationStart = 13000;
            public const int RaiserrorBusinessStart = 14000;

            // For THROW (50000+)
            public const int ThrowValidationStart = 50001;
            public const int ThrowBusinessStart = 51000;
        }
    }
}
