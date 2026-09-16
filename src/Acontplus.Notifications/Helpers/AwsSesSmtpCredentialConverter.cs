using System.Security.Cryptography;

namespace Acontplus.Notifications.Helpers;

/// <summary>
/// Helper to convert AWS IAM credentials into Amazon SES SMTP credentials using SigV4 algorithm.
/// </summary>
public static class AwsSesSmtpCredentialConverter
{
    /// <summary>
    /// Converts an AWS IAM access key and secret key into Amazon SES SMTP credentials.
    /// </summary>
    /// <param name="iamAccessKey">The AWS IAM access key ID.</param>
    /// <param name="iamSecretKey">The AWS IAM secret access key.</param>
    /// <param name="region">The AWS region (defaults to "us-east-1").</param>
    /// <returns>A tuple containing the SMTP username and generated SMTP password.</returns>
    public static (string smtpUsername, string smtpPassword) ConvertIamToSmtpCredentials(
        string iamAccessKey,
        string iamSecretKey,
        string region = "us-east-1")
    {
        // SMTP username is the same as the IAM access key
        var smtpUsername = iamAccessKey;

        // Constants as per AWS documentation
        var date = "11111111";
        var service = "ses";
        var terminal = "aws4_request";
        var message = "SendRawEmail";
        byte version = 0x04;

        // Step 1: Create the signature key following AWS algorithm
        var kSecret = Encoding.UTF8.GetBytes("AWS4" + iamSecretKey);
        var kDate = HmacSha256(date, kSecret);
        var kRegion = HmacSha256(region, kDate);
        var kService = HmacSha256(service, kRegion);
        var kTerminal = HmacSha256(terminal, kService);
        var kMessage = HmacSha256(message, kTerminal);

        // Step 2: Create signature and version - concatenate version byte with kMessage
        var signatureAndVersion = new byte[kMessage.Length + 1];
        signatureAndVersion[0] = version;
        Array.Copy(kMessage, 0, signatureAndVersion, 1, kMessage.Length);

        // Step 3: Base64 encode the result to get SMTP password
        var smtpPassword = Convert.ToBase64String(signatureAndVersion);

        return (smtpUsername, smtpPassword);
    }

    private static byte[] HmacSha256(string data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
