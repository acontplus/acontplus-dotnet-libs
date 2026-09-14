namespace Acontplus.Persistence.Common.Utilities;

/// <summary>
/// Provides OpenTelemetry / Activity diagnostic tracing sources for persistence repositories.
/// </summary>
public static class DiagnosticConfig
{
    /// <summary>
    /// Gets the repository activity source for tracing database operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Repository");
}
