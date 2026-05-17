namespace Acontplus.Core.Abstractions.Context;

/// <summary>
/// Provides access to the current user's context information including identity and claims.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    /// <returns>The user's unique identifier.</returns>
    int GetUserId();

    /// <summary>
    /// Gets the value of a specific claim for the current user.
    /// </summary>
    /// <typeparam name="T">The type to convert the claim value to.</typeparam>
    /// <param name="claimName">The name of the claim to retrieve.</param>
    /// <returns>The claim value converted to the specified type.</returns>
    T GetClaimValue<T>(string claimName);

    /// <summary>
    /// Gets the username of the current user.
    /// </summary>
    /// <returns>The user's username.</returns>
    string GetUserName();

    /// <summary>
    /// Gets the email address of the current user.
    /// </summary>
    /// <returns>The user's email address.</returns>
    string GetEmail();

    /// <summary>
    /// Gets the role name of the current user.
    /// </summary>
    /// <returns>The user's role name.</returns>
    string GetRoleName();
}
