namespace Acontplus.Services.Extensions.Context;

/// <summary>
/// Extension methods for storing and retrieving ambient request context metadata on <see cref="HttpContext.Items"/>.
/// </summary>
public static class HttpContextExtensions
{
    private const string RequestIdKey = "RequestId";
    private const string CorrelationIdKey = "CorrelationId";
    private const string TenantIdKey = "TenantId";
    private const string ClientIdKey = "ClientId";
    private const string IssuerKey = "Issuer";
    private const string DeviceTypeKey = "DeviceType";
    private const string IsMobileRequestKey = "IsMobileRequest";

    /// <summary>
    /// Stores the request identifier in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="requestId">The request identifier.</param>
    public static void SetRequestId(this HttpContext context, string requestId) =>
        context.Items[RequestIdKey] = requestId;

    /// <summary>
    /// Retrieves the request identifier from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The request identifier, or null if not set.</returns>
    public static string? GetRequestId(this HttpContext context) =>
        context.Items.TryGetValue(RequestIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Stores the correlation identifier in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    public static void SetCorrelationId(this HttpContext context, string correlationId) =>
        context.Items[CorrelationIdKey] = correlationId;

    /// <summary>
    /// Retrieves the correlation identifier from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation identifier, or null if not set.</returns>
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Stores the tenant identifier in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    public static void SetTenantId(this HttpContext context, string tenantId) =>
        context.Items[TenantIdKey] = tenantId;

    /// <summary>
    /// Retrieves the tenant identifier from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant identifier, or null if not set.</returns>
    public static string? GetTenantId(this HttpContext context) =>
        context.Items.TryGetValue(TenantIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Stores the client identifier in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="clientId">The client identifier.</param>
    public static void SetClientId(this HttpContext context, string? clientId) =>
        context.Items[ClientIdKey] = clientId;

    /// <summary>
    /// Retrieves the client identifier from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client identifier, or null if not set.</returns>
    public static string? GetClientId(this HttpContext context) =>
        context.Items.TryGetValue(ClientIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Stores the token issuer in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="issuer">The issuer string.</param>
    public static void SetIssuer(this HttpContext context, string? issuer) =>
        context.Items[IssuerKey] = issuer;

    /// <summary>
    /// Retrieves the token issuer from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The issuer string, or null if not set.</returns>
    public static string? GetIssuer(this HttpContext context) =>
        context.Items.TryGetValue(IssuerKey, out var value) ? value as string : null;

    /// <summary>
    /// Stores the detected device type string representation in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="deviceType">The detected device type.</param>
    public static void SetDeviceType(this HttpContext context, DeviceType deviceType) =>
        context.Items[DeviceTypeKey] = deviceType.ToString().ToLowerInvariant();

    /// <summary>
    /// Retrieves the raw device type string from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The device type string, or null if not set.</returns>
    public static string? GetDeviceTypeString(this HttpContext context) =>
        context.Items.TryGetValue(DeviceTypeKey, out var value) ? value as string : null;

    /// <summary>
    /// Retrieves the parsed <see cref="DeviceType"/> enum from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The parsed device type, or null if not set or invalid.</returns>
    public static DeviceType? GetDeviceType(this HttpContext context)
    {
        if (context.Items.TryGetValue(DeviceTypeKey, out var value) &&
            value is string typeString &&
            Enum.TryParse<DeviceType>(typeString, ignoreCase: true, out var deviceType))
        {
            return deviceType;
        }

        return null;
    }

    /// <summary>
    /// Stores whether the request originated from a mobile device in <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="isMobileRequest">True if mobile request; otherwise false.</param>
    public static void SetIsMobileRequest(this HttpContext context, bool isMobileRequest)
    {
        context.Items["FromMobile"] = isMobileRequest; // Legacy
        context.Items[IsMobileRequestKey] = isMobileRequest;
    }

    /// <summary>
    /// Retrieves whether the request originated from a mobile device from <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if mobile, false if not, or null if not set.</returns>
    public static bool? GetIsMobileRequest(this HttpContext context) =>
        context.Items.TryGetValue(IsMobileRequestKey, out var value) ? value as bool? : null;
}