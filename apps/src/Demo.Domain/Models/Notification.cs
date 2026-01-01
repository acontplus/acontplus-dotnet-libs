namespace Demo.Domain.Models;

public class Notification
{
    public bool HasFile { get; set; }
    public required string IsReport { get; set; }
    public required Dictionary<string, object> ReportParams { get; set; }
    public required Dictionary<string, object> SpParams { get; set; }
    public bool WithTableNames { get; set; } = false;
}
