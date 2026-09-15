namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Represents a domain exception translated from a SQL error.
/// </summary>
/// <param name="sqlErrorInfo">The SQL error info containing error type, code, message, and inner exception.</param>
public class SqlDomainException(SqlErrorInfo sqlErrorInfo)
    : DomainException(sqlErrorInfo.ErrorType, sqlErrorInfo.Code, sqlErrorInfo.Message, sqlErrorInfo.Exception);
