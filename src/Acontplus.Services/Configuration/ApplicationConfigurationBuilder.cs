using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

namespace Acontplus.Services.Configuration;

/// <summary>
/// Builds the merged <see cref="IConfiguration"/> for every AcontPlus service.
/// </summary>
/// <remarks>
/// Priority order (lowest → highest, last wins):
/// <list type="number">
///   <item><c>appsettings.json</c></item>
///   <item><c>appsettings.{Environment}.json</c></item>
///   <item>Environment variables – for operational overrides and path resolution</item>
///   <item>Shared settings file – <c>sharedsettings.{Environment}.json</c> from the platform shared folder</item>
///   <item>Azure Key Vault – secrets only; activated when <c>KeyVault:VaultUri</c> or the
///         <c>KEYVAULT_URI</c> environment variable is set</item>
/// </list>
///
/// <para><b>Local development:</b> leave <c>KeyVault:VaultUri</c> unset and use User Secrets /
/// <c>sharedsettings.Development.json</c> for sensitive values.  When you do want to test against
/// the dev vault, run <c>az login</c> first — <see cref="DefaultAzureCredential"/> will pick up
/// those credentials automatically.</para>
///
/// <para><b>Azure-hosted environments (Staging / Production):</b> set <c>KeyVault:VaultUri</c>
/// in <c>appsettings.json</c> (or as the <c>KEYVAULT_URI</c> environment variable) and assign the
/// Managed Identity the <em>Key Vault Secrets User</em> role on the vault.  For user-assigned
/// Managed Identities, also set <c>KeyVault:ManagedIdentityClientId</c>.</para>
///
/// <para><b>Key Vault secret naming convention:</b> use double-dash as the hierarchy separator,
/// e.g. <c>JwtSettings--SecurityKey</c>, <c>ConnectionStrings--DefaultConnection</c>,
/// <c>AwsConfiguration--AWSSecretKey</c>.</para>
/// </remarks>
public static class ApplicationConfigurationBuilder
{
    private static string? GetPlatformSharedFolder(IConfiguration baseConfig)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return baseConfig.GetValue<string>("SharedPaths:Windows");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return baseConfig.GetValue<string>("SharedPaths:Linux");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return baseConfig.GetValue<string>("SharedPaths:OSX");

        return string.Empty;
    }

    public static IConfiguration Load()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(); // allows SHARED_SETTINGS_PATH / SharedPaths:* from env

        var baseConfig = builder.Build();

        // ── Shared settings file ─────────────────────────────────────────────
        var sharedFolder = Environment.GetEnvironmentVariable("SHARED_SETTINGS_PATH")
                           ?? GetPlatformSharedFolder(baseConfig);

        if (!string.IsNullOrEmpty(sharedFolder))
        {
            var sharedFile = Path.Combine(sharedFolder, $"sharedsettings.{environment}.json");
            if (File.Exists(sharedFile))
                builder.AddJsonFile(sharedFile, optional: true, reloadOnChange: true);
        }

        // ── Azure Key Vault ──────────────────────────────────────────────────
        // Resolved from: KEYVAULT_URI env var → KeyVault:VaultUri in appsettings
        var vaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI")
                       ?? baseConfig.GetValue<string>("KeyVault:VaultUri");

        if (!string.IsNullOrEmpty(vaultUri))
        {
            var managedIdentityClientId = baseConfig.GetValue<string>("KeyVault:ManagedIdentityClientId");

            var credentialOptions = new DefaultAzureCredentialOptions();
            if (!string.IsNullOrEmpty(managedIdentityClientId))
                credentialOptions.ManagedIdentityClientId = managedIdentityClientId;

            builder.AddAzureKeyVault(
                new Uri(vaultUri),
                new DefaultAzureCredential(credentialOptions),
                new KeyVaultSecretManager());
        }

        return builder.Build();
    }
}
