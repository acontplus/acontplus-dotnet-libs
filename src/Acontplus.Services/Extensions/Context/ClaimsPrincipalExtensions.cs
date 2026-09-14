namespace Acontplus.Services.Extensions.Context;

/// <summary>
/// Extension methods for extracting common claims from a <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the username from the <see cref="ClaimTypes.Name"/> claim.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>The username, or null if not found.</returns>
    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the email address from the <see cref="ClaimTypes.Email"/> claim.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>The email address, or null if not found.</returns>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email);
    }

    /// <summary>
    /// Gets the role name from the <see cref="ClaimTypes.Role"/> claim.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>The role name, or null if not found.</returns>
    public static string? GetRoleName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets the user ID from the <see cref="ClaimTypes.NameIdentifier"/> claim as an integer.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>The parsed user ID, or 0 if not found or invalid.</returns>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Retrieves the value of a specific claim from the ClaimsPrincipal and converts it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to which the claim value will be converted.</typeparam>
    /// <param name="user">The ClaimsPrincipal instance from which the claim will be retrieved.</param>
    /// <param name="claimName">The name of the claim to retrieve.</param>
    /// <returns>The value of the claim converted to the specified type, or the default value of the type if the claim does not exist or cannot be converted.</returns>
    public static T? GetClaimValue<T>(this ClaimsPrincipal user, string claimName)
    {
        var claim = user.FindFirst(claimName)?.Value;
        if (claim == null)
            return default;

        try
        {
            // Manejo de tipos comunes
            return typeof(T) == typeof(string)
                ? (T)(object)claim
                : typeof(T) == typeof(int)
                ? (T)(object)Convert.ToInt32(claim)
                : typeof(T) == typeof(long)
                ? (T)(object)Convert.ToInt64(claim)
                : typeof(T) == typeof(bool)
                ? (T)(object)Convert.ToBoolean(claim)
                : typeof(T) == typeof(Guid) ? (T)(object)Guid.Parse(claim) : (T)Convert.ChangeType(claim, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
