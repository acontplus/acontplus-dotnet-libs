namespace Acontplus.Core.Constants;

/// <summary>
/// Metadata keys for health check responses and diagnostics.
/// </summary>
public static class HealthCheckMetadataKeys
{
    // Circuit Breaker States
    /// <summary>
    /// Metadata key for the default circuit breaker state.
    /// </summary>
    public const string DefaultCircuit = "default";
    
    /// <summary>
    /// Metadata key for the API circuit breaker state.
    /// </summary>
    public const string ApiCircuit = "api";
    
    /// <summary>
    /// Metadata key for the database circuit breaker state.
    /// </summary>
    public const string DatabaseCircuit = "database";
    
    /// <summary>
    /// Metadata key for the external service circuit breaker state.
    /// </summary>
    public const string ExternalCircuit = "external";
    
    /// <summary>
    /// Metadata key for the authentication circuit breaker state.
    /// </summary>
    public const string AuthCircuit = "auth";

    // Health Check Diagnostics
    /// <summary>
    /// Metadata key for the timestamp of the last health check.
    /// </summary>
    public const string LastCheckTime = "lastCheckTime";
    
    /// <summary>
    /// Metadata key for the health check status.
    /// </summary>
    public const string Status = "status";
    
    /// <summary>
    /// Metadata key for the health check description.
    /// </summary>
    public const string Description = "description";
    
    /// <summary>
    /// Metadata key for the health check duration in milliseconds.
    /// </summary>
    public const string Duration = "durationMs";
    
    /// <summary>
    /// Metadata key for exception information from health checks.
    /// </summary>
    public const string Exception = "exception";

    // Resource States
    /// <summary>
    /// Metadata key for total memory available.
    /// </summary>
    public const string TotalMemory = "totalMemory";
    
    /// <summary>
    /// Metadata key for used memory amount.
    /// </summary>
    public const string UsedMemory = "usedMemory";
    
    /// <summary>
    /// Metadata key for free memory amount.
    /// </summary>
    public const string FreeMemory = "freeMemory";
    
    /// <summary>
    /// Metadata key for CPU usage percentage.
    /// </summary>
    public const string CpuUsage = "cpuUsage";
    
    /// <summary>
    /// Metadata key for active thread count.
    /// </summary>
    public const string ThreadCount = "threadCount";
}
