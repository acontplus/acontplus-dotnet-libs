namespace Acontplus.Services.Extensions.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        // Extract and validate configuration values
        var issuer = config["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is required");
        var securityKey = config["JwtSettings:SecurityKey"] ?? throw new InvalidOperationException("JWT SecurityKey is required");

        var audiences = GetValidAudiences(config);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Security best practices
                    RequireExpirationTime = true,
                    RequireAudience = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Enhanced security
                    RequireSignedTokens = true,
                    ValidateTokenReplay = true,

                    // Configuration values
                    ValidIssuer = issuer,
                    ValidAudiences = audiences, // Supports both single and multiple audiences
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(securityKey)),

                    // Clock skew for time validation
                    ClockSkew = TimeSpan.FromMinutes(
                        Convert.ToInt32(config["JwtSettings:ClockSkew"] ?? "5"))
                };

                // Enhanced JWT bearer options
                options.RequireHttpsMetadata = Convert.ToBoolean(
                    config["JwtSettings:RequireHttps"] ?? "true");
                options.SaveToken = false;
                options.IncludeErrorDetails = false;
            });

        // Authorization will be configured by AddAuthorizationPolicies() in ApplicationServiceExtensions

        return services;
    }

    private static string[] GetValidAudiences(IConfiguration configuration)
    {
        var audienceSection = configuration.GetSection("JwtSettings:Audience");
        if (audienceSection.Value is { } singleAudience)
        {
            return !string.IsNullOrWhiteSpace(singleAudience)
                ? [singleAudience.Trim()]
                : throw new InvalidOperationException("JWT Audience is required.");
        }

        var audiences = audienceSection.Get<string[]>()
            ?? throw new InvalidOperationException("JWT Audience is required.");

        if (audiences.Length == 0 || audiences.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("JWT Audience must contain at least one non-empty value.");
        }

        return audiences
            .Select(audience => audience.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
