namespace Acontplus.Core.Constants;

/// <summary>
/// Metadata keys for debug/diagnostic information in error responses.
/// These should only be included in Development environment or when explicitly enabled.
/// </summary>
public static class DebugMetadataKeys
{
    /// <summary>
    /// Key for the debug information container.
    /// </summary>
    public const string Debug = "debug";

    /// <summary>
    /// Key for the exception type name.
    /// </summary>
    public const string ExceptionType = "type";

    /// <summary>
    /// Key for the exception message.
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Key for the exception stack trace.
    /// </summary>
    public const string StackTrace = "stackTrace";

    /// <summary>
    /// Key for the inner exception details.
    /// </summary>
    public const string InnerException = "innerException";

    /// <summary>
    /// Key for the activity ID associated with the request.
    /// </summary>
    public const string ActivityId = "activityId";

    /// <summary>
    /// Key for the exception source.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Key for the target site where the exception occurred.
    /// </summary>
    public const string TargetSite = "targetSite";

    /// <summary>
    /// Key for the help link URL.
    /// </summary>
    public const string HelpLink = "helpLink";

    /// <summary>
    /// Key for additional exception data.
    /// </summary>
    public const string Data = "data";
}
