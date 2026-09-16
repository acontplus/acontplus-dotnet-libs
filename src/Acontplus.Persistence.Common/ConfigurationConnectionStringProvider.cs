namespace Acontplus.Persistence.Common;

/// <summary>
///     Default implementation of IConnectionStringProvider using IConfiguration.
///     Supports hierarchical, environment-based, and secure connection string resolution.
/// </summary>
/// <param name="configuration">The application configuration root.</param>
public class ConfigurationConnectionStringProvider(IConfiguration configuration) : IConnectionStringProvider
{
    /// <inheritdoc />
    public string GetConnectionString(string name) => configuration.GetConnectionString(name) ??
                                                      throw new InvalidOperationException(
                                                          $"Connection string '{name}' not found");
}
