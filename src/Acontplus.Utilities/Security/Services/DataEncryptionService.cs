namespace Acontplus.Utilities.Security.Services;

/// <summary>
/// Provides data encryption and decryption services using ASP.NET Core Data Protection.
/// </summary>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of <see cref="DataEncryptionService"/>.
    /// </summary>
    /// <param name="provider">The data protection provider.</param>
    /// <param name="configuration">Configuration containing 'DataProtection:ProtectorKey'.</param>
    public DataEncryptionService(IDataProtectionProvider provider, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(configuration);

        var protectorKey = configuration["DataProtection:ProtectorKey"]
            ?? throw new InvalidOperationException("Configuration key 'DataProtection:ProtectorKey' is required for DataEncryptionService.");
        _protector = provider.CreateProtector(protectorKey);
    }

    public byte[] EncryptToBytes(string plainText)
    {
        return _protector.Protect(Encoding.UTF8.GetBytes(plainText));
    }

    public string DecryptFromBytes(byte[] encryptedData)
    {
        return Encoding.UTF8.GetString(_protector.Unprotect(encryptedData));
    }
}
