namespace Acontplus.Persistence.PostgreSQL.Utilities;

// Extension method for PostgreSQL snake_case naming convention
/// <summary>
/// String extension methods for PostgreSQL snake_case naming conventions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a PascalCase or camelCase string to snake_case.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The snake_case representation of the input.</returns>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
