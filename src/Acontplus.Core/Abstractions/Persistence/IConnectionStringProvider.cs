namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Provides access to connection strings for data persistence.
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Gets the connection string for the specified connection name.
    /// </summary>
    /// <param name="name">The name of the connection.</param>
    /// <returns>The connection string associated with the specified name.</returns>
    string GetConnectionString(string name);
}
