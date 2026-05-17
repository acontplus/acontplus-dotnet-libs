namespace Acontplus.Core.Validation;

/// <summary>
/// Common data-validation helpers for business and infrastructure use.
/// All methods are pure (no side effects) and thread-safe.
/// </summary>
public static class DataValidation
{
    // ── Precompiled regex patterns ───────────────────────────────────────────────

    // RFC 5322-simplified; covers the vast majority of real-world addresses.
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250));

    private static readonly Regex UrlRegex =
        new(@"^https?://[^\s/$.?#].[^\s]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250));

    // E.164-ish: optional leading +, 7–15 digits, optional spaces/dashes/dots
    private static readonly Regex PhoneRegex =
        new(@"^\+?[\d\s\-\.\(\)]{7,20}$",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(250));

    private static readonly Regex SpecialCharsRegex =
        new(@"[^0-9A-Za-z _-]",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(250));

    // ── String / format validators ───────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if the string is a valid e-mail address.</summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { return EmailRegex.IsMatch(email); }
        catch (RegexMatchTimeoutException) { return false; }
    }

    /// <summary>Returns <c>true</c> if the string is a valid HTTP/HTTPS URL.</summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        try { return UrlRegex.IsMatch(url) && Uri.TryCreate(url, UriKind.Absolute, out _); }
        catch (RegexMatchTimeoutException) { return false; }
    }

    /// <summary>Returns <c>true</c> if the string looks like a phone number (7–20 digits/symbols).</summary>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
        try { return PhoneRegex.IsMatch(phoneNumber); }
        catch (RegexMatchTimeoutException) { return false; }
    }

    /// <summary>Returns <c>true</c> if the string is valid JSON.</summary>
    public static bool IsValidJson(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString)) return false;
        try
        {
            using var _ = JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException) { return false; }
    }

    /// <summary>Returns <c>true</c> if the string is well-formed XML.</summary>
    public static bool IsValidXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return false;
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Strips all characters that are not alphanumeric, spaces, underscores, or hyphens.</summary>
    public static string RemoveSpecialCharacters(string text)
        => string.IsNullOrEmpty(text) ? text : SpecialCharsRegex.Replace(text, string.Empty);

    // ── IP address helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates an IP address string and returns it if valid; otherwise returns <c>"0.0.0.0"</c>.
    /// Handles forwarded-for headers that may be prefixed with <c>"::ffff:"</c> (IPv4-mapped IPv6).
    /// </summary>
    public static string ValidateIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return "0.0.0.0";

        // Strip well-known IPv4-mapped IPv6 prefix "::ffff:"
        var candidate = ipAddress.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase)
            ? ipAddress[7..]
            : ipAddress;

        return System.Net.IPAddress.TryParse(candidate, out _) ? candidate : "0.0.0.0";
    }

    // ── ADO.NET / DataSet helpers ────────────────────────────────────────────────

    /// <summary>Returns <c>DBNull.Value</c> if <paramref name="obj"/> is <c>null</c>; otherwise returns <paramref name="obj"/>.</summary>
    public static object ToDbNullOrDefault(this object? obj) => obj ?? DBNull.Value;

    /// <summary>Returns <c>true</c> if the <see cref="DataTable"/> is <c>null</c> or contains no rows.</summary>
    public static bool DataTableIsNull(DataTable? dt) => dt is not { Rows.Count: > 0 };

    /// <summary>
    /// Returns <c>true</c> if the <see cref="DataSet"/> is <c>null</c> or contains no tables (with rows).
    /// When <paramref name="removeEmptyTables"/> is <c>true</c>, tables with zero rows are removed first.
    /// </summary>
    public static bool DataSetIsNull(DataSet? ds, bool removeEmptyTables = false)
    {
        if (ds == null) return true;

        if (removeEmptyTables)
        {
            var toRemove = ds.Tables.Cast<DataTable>()
                             .Where(dt => dt.Rows.Count == 0)
                             .ToList();
            foreach (var dt in toRemove)
                ds.Tables.Remove(dt);
        }

        return ds.Tables.Count == 0;
    }
}
