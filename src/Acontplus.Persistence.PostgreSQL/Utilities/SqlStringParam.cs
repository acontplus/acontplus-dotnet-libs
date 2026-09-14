namespace Acontplus.Persistence.PostgreSQL.Utilities;

/// <summary>
/// Provides utilities for sanitizing SQL string parameters to prevent SQL injection.
/// </summary>
public static partial class SqlStringParam
{
    [GeneratedRegex(@";|=|<|>| or | and |select| insert | update | drop | xp_ | --| exec", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SanitizeRegex();

    /// <summary>
    /// Removes common SQL injection patterns from the input string by replacing them with spaces.
    /// </summary>
    /// <param name="input">The raw input string to sanitize.</param>
    /// <returns>The sanitized string with dangerous patterns replaced by spaces.</returns>
    public static string Sanitize(string input)
    {
        return SanitizeRegex().Replace(input, MatchEvaluatorHandler);
    }

    private static string MatchEvaluatorHandler(Match match)
    {
        //Replace the matched items with a blank string of
        //equal length
        return new string(' ', match.Length);
    }
}
