using System.Security.Cryptography;

namespace Acontplus.Notifications.Helpers;

public static class AwsSesSmtpCredentialConverter
{
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
