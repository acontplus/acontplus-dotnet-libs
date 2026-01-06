namespace Acontplus.Utilities.Security.Services;

/// <summary>
/// Implementation of MAC (Message Authentication Code) security service using HMAC algorithms.
/// Provides secure message integrity and authenticity verification for API communications.
/// </summary>
public class MacSecurityService : IMacSecurityService
{
    /// <summary>
    /// Generates a MAC (HMAC-SHA256) for the given data using the provided secret key.
    /// </summary>
    /// <param name="data">The data to generate a MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    /// <exception cref="ArgumentNullException">Thrown when data or secretKey is null or empty</exception>
    public string GenerateMac(string data, string secretKey)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data), "Data cannot be null or empty");
        
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty");

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Generates a MAC (HMAC-SHA512) for the given data using the provided secret key.
    /// Provides stronger security than SHA256 at the cost of a larger signature.
    /// </summary>
    /// <param name="data">The data to generate a MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    /// <exception cref="ArgumentNullException">Thrown when data or secretKey is null or empty</exception>
    public string GenerateMacSha512(string data, string secretKey)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data), "Data cannot be null or empty");
        
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty");

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies that a MAC signature matches the expected MAC for the given data and secret key.
    /// Uses timing-safe comparison (CryptographicOperations.FixedTimeEquals) to prevent timing attacks.
    /// </summary>
    /// <param name="data">The original data</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    public bool VerifyMac(string data, string providedMac, string secretKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(providedMac) || string.IsNullOrEmpty(secretKey))
            return false;

        try
        {
            var expectedMac = GenerateMac(data, secretKey);
            var expectedBytes = Convert.FromBase64String(expectedMac);
            var providedBytes = Convert.FromBase64String(providedMac);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch
        {
            // If any error occurs during verification (e.g., invalid Base64), return false
            return false;
        }
    }

    /// <summary>
    /// Verifies that a MAC signature (SHA512) matches the expected MAC for the given data and secret key.
    /// Uses timing-safe comparison (CryptographicOperations.FixedTimeEquals) to prevent timing attacks.
    /// </summary>
    /// <param name="data">The original data</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    public bool VerifyMacSha512(string data, string providedMac, string secretKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(providedMac) || string.IsNullOrEmpty(secretKey))
            return false;

        try
        {
            var expectedMac = GenerateMacSha512(data, secretKey);
            var expectedBytes = Convert.FromBase64String(expectedMac);
            var providedBytes = Convert.FromBase64String(providedMac);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch
        {
            // If any error occurs during verification (e.g., invalid Base64), return false
            return false;
        }
    }

    /// <summary>
    /// Generates a MAC for JSON data (automatically serializes object to JSON before computing MAC).
    /// Uses System.Text.Json for serialization with default options.
    /// </summary>
    /// <param name="data">The object to serialize and generate MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    /// <exception cref="ArgumentNullException">Thrown when data or secretKey is null</exception>
    public string GenerateMacForJson(object data, string secretKey)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Data cannot be null");
        
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty");

        var jsonData = JsonSerializer.Serialize(data);
        return GenerateMac(jsonData, secretKey);
    }

    /// <summary>
    /// Verifies a MAC signature for JSON data.
    /// Uses System.Text.Json for serialization with default options.
    /// </summary>
    /// <param name="data">The object to serialize and verify MAC for</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    public bool VerifyMacForJson(object data, string providedMac, string secretKey)
    {
        if (data == null || string.IsNullOrEmpty(providedMac) || string.IsNullOrEmpty(secretKey))
            return false;

        try
        {
            var jsonData = JsonSerializer.Serialize(data);
            return VerifyMac(jsonData, providedMac, secretKey);
        }
        catch
        {
            return false;
        }
    }
}
