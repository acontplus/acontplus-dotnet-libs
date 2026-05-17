using System.Diagnostics;

namespace Acontplus.Persistence.SqlServer.Utilities;

/// <summary>
/// Provides OpenTelemetry diagnostic configuration for the SQL Server repository layer.
/// </summary>
public static class DiagnosticConfig
{
    /// <summary>
    /// The <see cref="ActivitySource"/> used to create diagnostic tracing activities.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Repository");
}
