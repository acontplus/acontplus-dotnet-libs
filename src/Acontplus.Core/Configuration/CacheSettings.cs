namespace Acontplus.Core.Configuration;

/// <summary>
/// Configuration settings for the application cache system.
/// </summary>
public sealed class CacheSettings
{
    /// <summary>
    /// The configuration section name for cache settings.
    /// </summary>
    public const string SectionName = "CacheSettings";

    /// <summary>
    /// Gets the maximum size limit of the cache in megabytes.
    /// </summary>
    public int SizeLimitMb { get; init; } = 100;

    /// <summary>
    /// Gets the percentage of items to remove when the cache reaches its size limit.
    /// </summary>
    public double CompactionPercentage { get; init; } = 0.25;

    /// <summary>
    /// Gets the frequency in minutes for scanning and removing expired cache items.
    /// </summary>
    public int ExpirationScanFrequencyMinutes { get; init; } = 5;

    /// <summary>
    /// Gets the default expiration time in minutes for cached items.
    /// </summary>
    public int DefaultExpirationMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the predefined cache profile settings.
    /// </summary>
    public CacheProfileSettings Profiles { get; init; } = new();
}

/// <summary>
/// Predefined cache expiration profiles for different cache duration scenarios.
/// </summary>
public sealed class CacheProfileSettings
{
    /// <summary>
    /// Gets the expiration time in minutes for short-lived cache items.
    /// </summary>
    public int ShortExpirationMinutes { get; init; } = 5;

    /// <summary>
    /// Gets the expiration time in minutes for medium-lived cache items.
    /// </summary>
    public int MediumExpirationMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the expiration time in minutes for long-lived cache items.
    /// </summary>
    public int LongExpirationMinutes { get; init; } = 120;

    /// <summary>
    /// Gets the expiration time in minutes for very long-lived cache items.
    /// </summary>
    public int VeryLongExpirationMinutes { get; init; } = 1440;
}
