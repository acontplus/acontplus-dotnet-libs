namespace Acontplus.Utilities.Security.Helpers;

/// <summary>
/// Provides cryptographic helper methods for key derivation and HMAC computation.
/// </summary>
public static class CryptographyHelper
{
    // NIST SP 800-132 (2023) recommends ≥600,000 iterations for PBKDF2-HMAC-SHA256.
    private const int Pbkdf2Iterations = 600_000;

    /// <summary>
    /// Derives a cryptographic key from a passphrase and salt using PBKDF2 with HMAC-SHA256.
    /// </summary>
    /// <param name="passphrase">The secret passphrase.</param>
    /// <param name="keySize">The desired key size in bits (e.g., 256).</param>
    /// <param name="salt">Cryptographic salt bytes.</param>
    /// <returns>The derived key bytes.</returns>
    public static byte[] DeriveKey(string passphrase, int keySize, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            keySize / 8);
    }

    /// <summary>
    /// Computes an HMAC-SHA256 hash for the given data using the specified passphrase.
    /// </summary>
    /// <param name="passphrase">The secret passphrase.</param>
    /// <param name="data">The data bytes to hash.</param>
    /// <returns>The computed HMAC hash bytes.</returns>
    public static byte[] ComputeHmac(string passphrase, byte[] data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(passphrase));
        return hmac.ComputeHash(data);
    }
}
