namespace Acontplus.Utilities.IO;

/// <summary>
/// Provides helper methods for retrieving the current application environment.
/// </summary>
public static class EnvironmentHelper
{
    /// <summary>
    /// Gets the current ASP.NET Core environment as an <see cref="EnvironmentEnums"/> value.
    /// </summary>
    /// <returns>The current environment as an <see cref="EnvironmentEnums"/> enum.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the environment variable is not set.</exception>
    public static EnvironmentEnums GetEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        ArgumentNullException.ThrowIfNull(environmentName);

        return Enum.Parse<EnvironmentEnums>(environmentName);
    }
}
