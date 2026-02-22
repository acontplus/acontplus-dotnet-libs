namespace Acontplus.Utilities.Security.Services;

public class PasswordSecurityService(IDataEncryptionService dataEncryptionService) : IPasswordSecurityService
{
    public string GetDecryptedPassword(byte[] encryptedPassword)
    {
        return dataEncryptionService.DecryptFromBytes(encryptedPassword);
    }

    // OWASP recommends work factor ≥12; increase over time as hardware improves.
    private const int BcryptWorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);
    }

    public (byte[] EncryptedPassword, string PasswordHash) SetPassword(string password)
    {
        var encryptedPassword = dataEncryptionService.EncryptToBytes(password);
        var passwordHash = HashPassword(password);
        return (encryptedPassword, passwordHash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
