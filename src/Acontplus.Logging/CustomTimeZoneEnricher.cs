namespace Acontplus.Logging;

/// <summary>
/// Enriches log events with timestamps converted to a specific time zone.
/// </summary>
public class CustomTimeZoneEnricher : ILogEventEnricher
{
    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTimeZoneEnricher"/> class.
    /// </summary>
    /// <param name="timeZoneId">The time zone identifier to use for timestamp conversion.</param>
    public CustomTimeZoneEnricher(string timeZoneId)
    {
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _timeZone = TimeZoneInfo.Utc; // Fallback to UTC
        }
    }

    /// <summary>
    /// Enriches the log event with a custom timestamp property converted to the configured time zone.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The factory used to create the property.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(logEvent.Timestamp.UtcDateTime, _timeZone);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomTimestamp", localTime));
    }
}
