namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Represents a domain exception translated from a SQL error.
/// </summary>
public class SqlDomainException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDomainException"/> class from SQL error information.
    /// </summary>
    /// <param name="sqlErrorInfo">The SQL error info containing error type, code, message, and inner exception.</param>
    public SqlDomainException(SqlErrorInfo sqlErrorInfo)
        : base(sqlErrorInfo.ErrorType, sqlErrorInfo.Code, sqlErrorInfo.Message, sqlErrorInfo.Exception)
    {
    }
}
