namespace Acontplus.Persistence.PostgreSQL.Utilities;

/// <summary>
/// Provides utilities for sanitizing SQL string parameters to prevent SQL injection.
/// </summary>
public static class SqlStringParam
{
    /// <summary>
    /// Removes common SQL injection patterns from the input string by replacing them with spaces.
    /// </summary>
    /// <param name="input">The raw input string to sanitize.</param>
    /// <returns>The sanitized string with dangerous patterns replaced by spaces.</returns>
    public static string Sanitize(string input)
    {
        var expression =
            new Regex(@";|=|<|>| or | and |select
              | insert | update | drop | xp_ | --| exec"
            );

        var result =
            expression.Replace(input, MatchEvaluatorHandler);

        return result;
    }

    private static string MatchEvaluatorHandler(Match match)
    {
        //Replace the matched items with a blank string of
        //equal length
        return new string(' ', match.Length);
    }
}
