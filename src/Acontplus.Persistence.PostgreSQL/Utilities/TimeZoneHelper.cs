namespace Acontplus.Persistence.PostgreSQL.Utilities;

/// <summary>
/// Provides time zone conversion utilities for Ecuador and server local time.
/// </summary>
public static class TimeZoneHelper
{
    // Zona horaria de Ecuador (ECT - Ecuador Time)
    private static readonly TimeZoneInfo EcuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); // UTC-5

    // Zona horaria del servidor (configurable)
    private static readonly TimeZoneInfo ServerTimeZone = TimeZoneInfo.Local;

    /// <summary>
    /// OPCIÓN 1 (RECOMENDADA): Convertir UTC a zona horaria específica
    /// </summary>
    public static DateTime ToEcuadorTime(this DateTime utcDateTime)
    {
        return utcDateTime.Kind != DateTimeKind.Utc
            ? throw new ArgumentException("DateTime must be UTC", nameof(utcDateTime))
            : TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, EcuadorTimeZone);
    }

    /// <summary>
    /// Converts a nullable UTC <see cref="DateTime"/> to Ecuador time, returning <c>null</c> if the input is <c>null</c>.
    /// </summary>
    /// <param name="utcDateTime">The nullable UTC date and time.</param>
    /// <returns>The Ecuador local date and time, or <c>null</c>.</returns>
    public static DateTime? ToEcuadorTime(this DateTime? utcDateTime)
    {
        return utcDateTime?.ToEcuadorTime();
    }

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to the server's local time zone.
    /// </summary>
    /// <param name="utcDateTime">The UTC date and time to convert.</param>
    /// <returns>The date and time in the server's local time zone.</returns>
    public static DateTime ToServerTime(this DateTime utcDateTime)
    {
        return utcDateTime.Kind != DateTimeKind.Utc
            ? throw new ArgumentException("DateTime must be UTC", nameof(utcDateTime))
            : TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ServerTimeZone);
    }

    /// <summary>
    /// Convertir de zona horaria local a UTC (para guardar en BD)
    /// </summary>
    public static DateTime FromEcuadorTimeToUtc(DateTime ecuadorDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(ecuadorDateTime, EcuadorTimeZone);
    }

    /// <summary>
    /// Obtener la hora actual en Ecuador
    /// </summary>
    public static DateTime NowInEcuador => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EcuadorTimeZone);

    /// <summary>
    /// Obtener la hora actual del servidor
    /// </summary>
    public static DateTime NowInServer => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServerTimeZone);
}
