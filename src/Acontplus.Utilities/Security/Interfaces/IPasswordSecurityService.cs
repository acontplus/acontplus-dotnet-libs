namespace Acontplus.Utilities.Security.Interfaces;

/// <summary>
/// Provides secure password hashing and verification services.
/// </summary>
public interface IPasswordSecurityService
{
    /// <summary>
    /// Computes a one-way BCrypt salted hash of the provided password.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>The salted BCrypt hash string.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plaintext password matches an existing BCrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password candidate.</param>
    /// <param name="hashedPassword">The stored BCrypt hash string.</param>
    /// <returns><see langword="true"/> if the password matches; otherwise, <see langword="false"/>.</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Sets a password by generating both an encrypted reversible representation and a BCrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>A tuple containing the encrypted bytes and the BCrypt hash.</returns>
    [Obsolete("Reversible password storage is deprecated for security reasons (OWASP Password Storage Cheat Sheet / Sonar S5344 / CWE-256). Passwords must never be reversible. Use HashPassword instead.")]
    (byte[] EncryptedPassword, string PasswordHash) SetPassword(string password);

    /// <summary>
    /// Decrypts reversible password bytes into plaintext.
    /// </summary>
    /// <param name="encryptedPassword">The encrypted password bytes.</param>
    /// <returns>The decrypted plaintext password.</returns>
    [Obsolete("Reversible password storage is deprecated for security reasons (OWASP Password Storage Cheat Sheet / Sonar S5344 / CWE-256). Storing reversible passwords exposes all credentials if keys are compromised.")]
    string GetDecryptedPassword(byte[] encryptedPassword);
}

