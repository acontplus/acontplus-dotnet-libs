namespace Acontplus.Utilities.Security.Interfaces;

/// <summary>
/// Defines asynchronous methods for encrypting and decrypting sensitive data.
/// </summary>
public interface ISensitiveDataEncryptionService
{
    /// <summary>
    /// Asynchronously encrypts the provided plaintext string using AES-256-GCM (authenticated encryption)
    /// and returns the encrypted data as a byte array.
    /// </summary>
    /// <param name="passphrase">The passphrase used to derive the AES-256 key via PBKDF2-HMAC-SHA256.</param>
    /// <param name="data">The plaintext string to encrypt.</param>
    /// <returns>A Task containing a byte array with layout: Salt (16) + Nonce (12) + Tag (16) + Ciphertext.</returns>
    Task<byte[]> EncryptToBytesAsync(string passphrase, string data);

    /// <summary>
    /// Asynchronously decrypts the provided byte array using AES-256-GCM and returns the decrypted plaintext string.
    /// Throws <see cref="System.Security.Cryptography.CryptographicException"/> if authentication tag verification fails.
    /// </summary>
    /// <param name="passphrase">The passphrase (must match the one used for encryption).</param>
    /// <param name="encryptedData">The byte array with layout: Salt (16) + Nonce (12) + Tag (16) + Ciphertext.</param>
    /// <returns>A Task containing the decrypted plaintext string.</returns>
    Task<string> DecryptFromBytesAsync(string passphrase, byte[] encryptedData);
}
