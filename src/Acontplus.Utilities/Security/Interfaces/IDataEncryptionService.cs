namespace Acontplus.Utilities.Security.Interfaces;

/// <summary>
/// Defines contracts for encrypting and decrypting sensitive data to and from byte arrays.
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string into an encrypted byte array.
    /// </summary>
    /// <param name="plainText">The plain text string to encrypt.</param>
    /// <returns>The encrypted byte array.</returns>
    byte[] EncryptToBytes(string plainText);

    /// <summary>
    /// Decrypts an encrypted byte array back into its original plain text string.
    /// </summary>
    /// <param name="encryptedData">The encrypted byte array.</param>
    /// <returns>The decrypted plain text string.</returns>
    string DecryptFromBytes(byte[] encryptedData);
}
