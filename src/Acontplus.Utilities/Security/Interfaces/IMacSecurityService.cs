namespace Acontplus.Utilities.Security.Interfaces;

/// <summary>
/// Service for generating and verifying Message Authentication Codes (MAC) using HMAC algorithms.
/// Provides integrity and authenticity verification for API messages and data.
/// </summary>
public interface IMacSecurityService
{
    /// <summary>
    /// Generates a MAC (HMAC-SHA256) for the given data using the provided secret key.
    /// </summary>
    /// <param name="data">The data to generate a MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    string GenerateMac(string data, string secretKey);

    /// <summary>
    /// Generates a MAC (HMAC-SHA512) for the given data using the provided secret key.
    /// </summary>
    /// <param name="data">The data to generate a MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    string GenerateMacSha512(string data, string secretKey);

    /// <summary>
    /// Verifies that a MAC signature matches the expected MAC for the given data and secret key.
    /// Uses timing-safe comparison to prevent timing attacks.
    /// </summary>
    /// <param name="data">The original data</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    bool VerifyMac(string data, string providedMac, string secretKey);

    /// <summary>
    /// Verifies that a MAC signature (SHA512) matches the expected MAC for the given data and secret key.
    /// Uses timing-safe comparison to prevent timing attacks.
    /// </summary>
    /// <param name="data">The original data</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    bool VerifyMacSha512(string data, string providedMac, string secretKey);

    /// <summary>
    /// Generates a MAC for JSON data (automatically serializes object to JSON before computing MAC).
    /// </summary>
    /// <param name="data">The object to serialize and generate MAC for</param>
    /// <param name="secretKey">The secret key to use for HMAC generation</param>
    /// <returns>Base64-encoded MAC signature</returns>
    string GenerateMacForJson(object data, string secretKey);

    /// <summary>
    /// Verifies a MAC signature for JSON data.
    /// </summary>
    /// <param name="data">The object to serialize and verify MAC for</param>
    /// <param name="providedMac">The MAC signature to verify (Base64-encoded)</param>
    /// <param name="secretKey">The secret key used for MAC generation</param>
    /// <returns>True if the MAC is valid, false otherwise</returns>
    bool VerifyMacForJson(object data, string providedMac, string secretKey);
}
