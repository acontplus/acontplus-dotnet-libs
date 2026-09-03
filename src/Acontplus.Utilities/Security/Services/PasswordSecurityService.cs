namespace Acontplus.Utilities.Security.Services;

/// <summary>
/// Implements password security services using BCrypt hashing.
/// </summary>
/// <param name="dataEncryptionService">The underlying data encryption service used for legacy decryption.</param>
public class PasswordSecurityService(IDataEncryptionService dataEncryptionService) : IPasswordSecurityService
{
    // OWASP recommends work factor ≥12; increase over time as hardware improves.
    private const int BcryptWorkFactor = 12;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    /// <inheritdoc />
    [Obsolete("Reversible password storage is deprecated for security reasons (OWASP Password Storage Cheat Sheet / Sonar S5344 / CWE-256). Passwords must never be reversible. Use HashPassword instead.")]
    public (byte[] EncryptedPassword, string PasswordHash) SetPassword(string password)
    {
        var encryptedPassword = dataEncryptionService.EncryptToBytes(password);
        var passwordHash = HashPassword(password);
        return (encryptedPassword, passwordHash);
    }

    /// <inheritdoc />
    [Obsolete("Reversible password storage is deprecated for security reasons (OWASP Password Storage Cheat Sheet / Sonar S5344 / CWE-256). Storing reversible passwords exposes all credentials if keys are compromised.")]
    public string GetDecryptedPassword(byte[] encryptedPassword)
    {
        return dataEncryptionService.DecryptFromBytes(encryptedPassword);
    }
}
