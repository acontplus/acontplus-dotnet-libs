namespace Acontplus.Utilities.Time;

/// <summary>
/// Provides date and time utility functions localized to Ecuador.
/// </summary>
public static class DateUtils
{
    /// <summary>
    /// Gets the current date and time converted to the Ecuador standard time zone (SA Pacific Standard Time).
    /// </summary>
    /// <returns>A <see cref="DateTime"/> representing the current time in Ecuador.</returns>
    public static DateTime GetEcuadorDate()
    {
        var currentTime = DateTime.Now;
        var ecuadorZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var ecuadorTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(currentTime, ecuadorZone.Id);
        return ecuadorTime;
    }
}
