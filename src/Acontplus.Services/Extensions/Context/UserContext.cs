namespace Acontplus.Services.Extensions.Context;

/// <summary>
/// Provides access to user claims and identity information from the current HTTP context.
/// </summary>
public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    /// <summary>
    /// Gets the user ID from the current user claims.
    /// </summary>
    public int GetUserId() => httpContextAccessor.HttpContext!.User.GetUserId();

    /// <summary>
    /// Gets a claim value of the specified type from the current user claims.
    /// </summary>
    /// <typeparam name="T">The type of the claim value.</typeparam>
    /// <param name="claimName">The name of the claim.</param>
    public T GetClaimValue<T>(string claimName) => httpContextAccessor.HttpContext!.User.GetClaimValue<T>(claimName) ?? default(T)!;

    /// <summary>
    /// Gets the user name from the current user claims.
    /// </summary>
    public string GetUserName() => httpContextAccessor.HttpContext?.User.GetUsername() ?? string.Empty;

    /// <summary>
    /// Gets the email from the current user claims.
    /// </summary>
    public string GetEmail() => httpContextAccessor.HttpContext?.User.GetEmail() ?? string.Empty;

    /// <summary>
    /// Gets the role name from the current user claims.
    /// </summary>
    public string GetRoleName() => httpContextAccessor.HttpContext?.User.GetRoleName() ?? string.Empty;
}
