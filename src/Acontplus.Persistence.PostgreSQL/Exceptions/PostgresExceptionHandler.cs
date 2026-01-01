using System.Runtime.CompilerServices;

namespace Acontplus.Persistence.PostgreSQL.Exceptions;

public static class PostgresExceptionHandler
{
    public static bool IsTransientException(NpgsqlException ex)
    {
        // 40001: serialization_failure, 40P01: deadlock_detected, 23505: unique_violation, 23503: foreign_key_violation
        return ex.SqlState is "40001" or "40P01" or "23505" or "23503" || (ex.SqlState?.StartsWith("08") ?? false);
    }

    public static SqlErrorInfo MapSqlException(NpgsqlException ex)
    {
        if (ex.SqlState == "23505" && ex.Message.Contains("duplicate key"))
        {
            return new SqlErrorInfo(
                ErrorType.Conflict,
                "PG_DUPLICATE_KEY",
                "Duplicate key violation",
                ex);
        }
        return ex.SqlState == "23503"
            ? new SqlErrorInfo(
                ErrorType.Conflict,
                "PG_FOREIGN_KEY_VIOLATION",
                "Foreign key constraint violation",
                ex)
            : ex.SqlState == "23502"
            ? new SqlErrorInfo(
                ErrorType.Validation,
                "PG_NOT_NULL_VIOLATION",
                "Null value in column violates not-null constraint",
                ex)
            : ex.SqlState == "22001"
            ? new SqlErrorInfo(
                ErrorType.Validation,
                "PG_STRING_TOO_LONG",
                "String data, right truncated",
                ex)
            : ex.SqlState == "40001"
            ? new SqlErrorInfo(
                ErrorType.Conflict,
                "PG_SERIALIZATION_FAILURE",
                "Serialization failure (deadlock or concurrency conflict)",
                ex)
            : ex.SqlState == "40P01"
            ? new SqlErrorInfo(
                ErrorType.Conflict,
                "PG_DEADLOCK_DETECTED",
                "Deadlock detected",
                ex)
            : ex.SqlState == "28P01"
            ? new SqlErrorInfo(
                ErrorType.Unauthorized,
                "PG_AUTH_FAILED",
                "Authentication failed",
                ex)
            : new SqlErrorInfo(
            ErrorType.Internal,
            $"PG_ERROR_{ex.SqlState}",
            ex.Message,
            ex);
    }

    public static void HandleSqlException(
        NpgsqlException ex,
        ILogger logger,
        string operation,
        [CallerMemberName] string caller = "")
    {
        var errorInfo = MapSqlException(ex);
        var sqlStateHashCode = ex.SqlState?.GetHashCode() ?? 0;
        logger.Log(GetLogLevel(errorInfo.ErrorType),
            new EventId(sqlStateHashCode, errorInfo.Code),
            "Postgres Error in {Operation} called from {Caller}: {ErrorType} - {Message}",
            operation, caller, errorInfo.ErrorType, errorInfo.Message);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Postgres Error Details: {Details}", new
            {
                ex.SqlState,
                ex.Message,
                ex.StackTrace,
                ex.Source,
                TargetSite = ex.TargetSite?.ToString(),
                ex.Data,
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
}
