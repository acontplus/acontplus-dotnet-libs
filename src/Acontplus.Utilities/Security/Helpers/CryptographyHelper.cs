namespace Acontplus.Utilities.Security.Helpers;

public static class CryptographyHelper
{
    // NIST SP 800-132 (2023) recommends ≥600,000 iterations for PBKDF2-HMAC-SHA256.
    private const int Pbkdf2Iterations = 600_000;

    public static byte[] DeriveKey(string passphrase, int keySize, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            keySize / 8);
    }

    public static byte[] ComputeHmac(string passphrase, byte[] data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(passphrase));
        return hmac.ComputeHash(data);
    }
}
