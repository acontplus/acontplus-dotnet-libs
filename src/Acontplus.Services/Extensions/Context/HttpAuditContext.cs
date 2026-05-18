namespace Acontplus.Services.Extensions.Context;

/// <summary>
/// Resolves audit identity from the current HTTP request context and JWT claims.
/// Register as <c>Scoped</c> so each request gets a fresh snapshot of the caller's identity.
/// </summary>
public sealed class HttpAuditContext : IAuditContext
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IUserContext _userContext;

    public HttpAuditContext(IHttpContextAccessor accessor, IUserContext userContext)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <inheritdoc />
    public int? UserId => SafeGet(() => _userContext.GetUserId());

    /// <inheritdoc />
    public string? UserName => SafeGetString(() => _userContext.GetUserName());

    /// <inheritdoc />
    public int? UserRoleId => SafeGet(() => _userContext.GetClaimValue<int>("userRoleId"));

    /// <inheritdoc />
    /// <remarks>
    /// Relies on <c>IsMobileRequest</c> being set in <c>HttpContext.Items</c> by the device-detection
    /// middleware that is registered via <c>AddApplicationServices()</c>.
    /// </remarks>
    public bool IsMobile => _accessor.HttpContext?.GetIsMobileRequest() ?? false;

    // ------------------------------------------------------------------
    // Helpers – swallow exceptions so missing/unauthenticated requests
    // don't break SaveChanges for background or anonymous operations.
    // ------------------------------------------------------------------

    private static int? SafeGet(Func<int> getter)
    {
        try
        {
            var value = getter();
            return value > 0 ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeGetString(Func<string> getter)
    {
        try
        {
            var value = getter();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
