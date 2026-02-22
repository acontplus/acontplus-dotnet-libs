namespace Acontplus.Utilities.Security.Services;

/// <summary>
/// Provides authenticated encryption using AES-256-GCM (AEAD).
/// Layout of encrypted output: Salt (16 bytes) + Nonce (12 bytes) + Tag (16 bytes) + Ciphertext.
/// </summary>
public class SensitiveDataEncryptionService : ISensitiveDataEncryptionService
{
    private const int SaltSize = 16;  // 128-bit salt for PBKDF2
    private const int NonceSize = 12; // 96-bit nonce (NIST recommended for GCM)
    private const int TagSize = 16;   // 128-bit GCM authentication tag
    private const int KeySize = 256;  // AES-256

    /// <summary>
    /// Encrypts the provided data using AES-256-GCM (authenticated encryption) and returns the encrypted byte array.
    /// </summary>
    /// <param name="passphrase">The passphrase used to derive the encryption key via PBKDF2-HMAC-SHA256.</param>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <returns>A byte array containing Salt (16) + Nonce (12) + Tag (16) + Ciphertext.</returns>
    public async Task<byte[]> EncryptToBytesAsync(string passphrase, string data)
    {
        if (string.IsNullOrWhiteSpace(passphrase)) throw new ArgumentException("Passphrase cannot be null or empty.", nameof(passphrase));
        if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException("Data cannot be null or empty.", nameof(data));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = CryptographyHelper.DeriveKey(passphrase, KeySize, salt);

        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plaintext = Encoding.UTF8.GetBytes(data);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            // Layout: Salt (16) + Nonce (12) + Tag (16) + Ciphertext
            var result = new byte[SaltSize + NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
            Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize + TagSize, ciphertext.Length);

            return await Task.FromResult(result);
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
        }
    }

    /// <summary>
    /// Decrypts the provided byte array using AES-256-GCM and returns the plaintext string.
    /// Throws <see cref="CryptographicException"/> if the tag verification fails (data is tampered or passphrase is wrong).
    /// </summary>
    /// <param name="passphrase">The passphrase used to derive the decryption key (must match the passphrase used for encryption).</param>
    /// <param name="encryptedData">The byte array containing Salt (16) + Nonce (12) + Tag (16) + Ciphertext.</param>
    /// <returns>The decrypted plaintext string.</returns>
    public async Task<string> DecryptFromBytesAsync(string passphrase, byte[] encryptedData)
    {
        if (string.IsNullOrWhiteSpace(passphrase)) throw new ArgumentException("Passphrase cannot be null or empty.", nameof(passphrase));
        if (encryptedData == null || encryptedData.Length == 0) throw new ArgumentException("Encrypted data cannot be null or empty.", nameof(encryptedData));

        var minLength = SaltSize + NonceSize + TagSize;
        if (encryptedData.Length <= minLength)
            throw new ArgumentException("Encrypted data is invalid or corrupted.", nameof(encryptedData));

        var salt = encryptedData[..SaltSize];
        var nonce = encryptedData[SaltSize..(SaltSize + NonceSize)];
        var tag = encryptedData[(SaltSize + NonceSize)..(SaltSize + NonceSize + TagSize)];
        var ciphertext = encryptedData[(SaltSize + NonceSize + TagSize)..];

        var key = CryptographyHelper.DeriveKey(passphrase, KeySize, salt);

        try
        {
            var plaintextBytes = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(key, TagSize);
            // AesGcm.Decrypt throws CryptographicException on tag mismatch — no silent corruption
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return await Task.FromResult(Encoding.UTF8.GetString(plaintextBytes));
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
        }
    }
}
