namespace Acontplus.Persistence.PostgreSQL.Exceptions;

public class PostgresExceptionTranslator : ISqlExceptionTranslator
{
    public bool IsTransient(Exception ex)
    {
        if (ex == null) return false;

        // Check if it's directly a NpgsqlException
        if (ex is NpgsqlException npgsqlEx)
        {
            return PostgresExceptionHandler.IsTransientException(npgsqlEx);
        }

        // Check if it's wrapped in another exception
        var innerNpgsqlException = FindNpgsqlException(ex);
        return innerNpgsqlException != null && PostgresExceptionHandler.IsTransientException(innerNpgsqlException);
    }

    public DomainException Translate(Exception ex)
    {
        if (ex == null)
        {
            return new GenericDomainException(
                ErrorType.Internal,
                "NULL_EXCEPTION",
                "Cannot translate null exception",
                null);
        }

        try
        {
            // First, try to find NpgsqlException in the exception chain
            var npgsqlException = FindNpgsqlException(ex);

            if (npgsqlException != null)
            {
                var errorInfo = PostgresExceptionHandler.MapSqlException(npgsqlException);
                return new SqlDomainException(errorInfo);
            }

            // If no NpgsqlException found, check if it's already a SqlDomainException
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
    /// Recursively searches for NpgsqlException in the exception chain
    /// </summary>
    private static NpgsqlException? FindNpgsqlException(Exception? ex)
    {
        if (ex == null) return null;

        if (ex is NpgsqlException npgsqlEx)
        {
            return npgsqlEx;
        }

        // Check inner exception
        if (ex.InnerException != null)
        {
            return FindNpgsqlException(ex.InnerException);
        }

        // Check if it's an AggregateException
        if (ex is AggregateException aggregateEx)
        {
            foreach (var innerEx in aggregateEx.InnerExceptions)
            {
                var foundNpgsqlEx = FindNpgsqlException(innerEx);
                if (foundNpgsqlEx != null)
                {
                    return foundNpgsqlEx;
                }
            }
        }

        return null;
    }
}
