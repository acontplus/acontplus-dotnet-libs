namespace Acontplus.Persistence.Common;

/// <summary>
///     Default implementation of IConnectionStringProvider using IConfiguration.
///     Supports hierarchical, environment-based, and secure connection string resolution.
/// </summary>
public class ConfigurationConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationConnectionStringProvider"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    public ConfigurationConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string GetConnectionString(string name) => _configuration.GetConnectionString(name) ??
                                                      throw new InvalidOperationException(
                                                          $"Connection string '{name}' not found");
}
