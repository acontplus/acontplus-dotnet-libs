using Microsoft.Extensions.Configuration;

namespace Demo.Application.Configuration;

internal static class JwtTokenConfiguration
{
    internal static string GetRequiredAudience(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var audienceSection = configuration.GetSection("JwtSettings:Audience");
        if (audienceSection.Value is { } singleAudience)
        {
            return !string.IsNullOrWhiteSpace(singleAudience)
                ? singleAudience.Trim()
                : throw new InvalidOperationException("JWT Audience is required.");
        }

        var audiences = audienceSection.Get<string[]>()
            ?? throw new InvalidOperationException("JWT Audience is required.");

        if (audiences.Length != 1 || string.IsNullOrWhiteSpace(audiences[0]))
        {
            throw new InvalidOperationException(
                "JWT token issuance requires exactly one non-empty target audience.");
        }

        return audiences[0].Trim();
    }
}
