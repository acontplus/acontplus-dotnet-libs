namespace Acontplus.Persistence.SqlServer.Exceptions;

/// <summary>
/// Translates SQL Server exceptions into domain exceptions.
/// </summary>
public class SqlExceptionTranslator : ISqlExceptionTranslator
{
    /// <summary>
    /// Determines whether the exception represents a transient SQL Server error.
    /// </summary>
    /// <param name="ex">The exception to evaluate.</param>
    /// <returns><c>true</c> if the error is transient and the operation can be retried.</returns>
    public bool IsTransient(Exception ex)
    {
        if (ex == null)
        {
            return false;
        }

        // Check if it's directly a SqlException
        if (ex is SqlException sqlEx)
        {
            return SqlServerExceptionHandler.IsTransientException(sqlEx);
        }

        // Check if it's wrapped in another exception
        var innerSqlException = FindSqlException(ex);
        return innerSqlException != null && SqlServerExceptionHandler.IsTransientException(innerSqlException);
    }

    /// <summary>
    /// Translates an exception into a <see cref="DomainException"/>.
    /// </summary>
    /// <param name="ex">The exception to translate.</param>
    /// <returns>A <see cref="DomainException"/> representing the error.</returns>
    public DomainException Translate(Exception ex)
    {
        if (ex == null)
        {
            return new GenericDomainException(
                ErrorType.Internal,
                "NULL_EXCEPTION",
                "Cannot translate null exception");
        }

        try
        {
            // First, try to find SqlException in the exception chain
            var sqlException = FindSqlException(ex);

            if (sqlException != null)
            {
                var errorInfo = SqlServerExceptionHandler.MapSqlException(sqlException);
                return new SqlDomainException(errorInfo);
            }

            // If no SqlException found, check if it's already a SqlDomainException
            if (ex is SqlDomainException sqlDomainEx)
            {
                return sqlDomainEx;
            }

            // Fallback for other exceptions
            return new GenericDomainException(
                ErrorType.Internal,
                "UNKNOWN_ERROR",
                ex.Message,
                ex);
        }
        catch (Exception translationError)
        {
            // Fallback in case translation fails
            return new GenericDomainException(
                ErrorType.Internal,
                "TRANSLATION_FAILED",
                $"Failed to translate exception: {ex.Message}. Translation error: {translationError.Message}",
                ex);
        }
    }

    /// <summary>
    ///     Recursively searches for SqlException in the exception chain
    /// </summary>
    private static SqlException? FindSqlException(Exception? ex)
    {
        if (ex == null)
        {
            return null;
        }

        if (ex is SqlException sqlEx)
        {
            return sqlEx;
        }

        // Check inner exception
        if (ex.InnerException != null)
        {
            return FindSqlException(ex.InnerException);
        }

        // Check if it's an AggregateException
        if (ex is AggregateException aggregateEx)
        {
            foreach (var innerEx in aggregateEx.InnerExceptions)
            {
                var foundSqlEx = FindSqlException(innerEx);
                if (foundSqlEx != null)
                {
                    return foundSqlEx;
                }
            }
        }

        return null;
    }
}
