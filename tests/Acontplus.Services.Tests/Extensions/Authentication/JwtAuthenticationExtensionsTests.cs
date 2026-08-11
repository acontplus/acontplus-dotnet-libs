using Acontplus.Services.Extensions.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acontplus.Services.Tests.Extensions.Authentication;

public sealed class JwtAuthenticationExtensionsTests
{
    [Fact]
    public void AddJwtAuthentication_WithSingleTrimmedAudience_RequiresAndValidatesAudience()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://issuer.example",
            ["JwtSettings:SecurityKey"] = "a-secret-key-that-is-at-least-thirty-two-characters",
            ["JwtSettings:Audience"] = " https://api.example ",
        });

        // Act
        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        // Assert
        Assert.True(options.TokenValidationParameters.RequireAudience);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.Equal(["https://api.example"], options.TokenValidationParameters.ValidAudiences);
    }

    [Fact]
    public void AddJwtAuthentication_WithMultipleAudiences_ValidatesEveryConfiguredResource()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://issuer.example",
            ["JwtSettings:SecurityKey"] = "a-secret-key-that-is-at-least-thirty-two-characters",
            ["JwtSettings:Audience:0"] = "https://api-one.example",
            ["JwtSettings:Audience:1"] = " https://api-two.example ",
        });

        // Act
        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        // Assert
        Assert.Equal(
            ["https://api-one.example", "https://api-two.example"],
            options.TokenValidationParameters.ValidAudiences);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddJwtAuthentication_WithMissingOrBlankAudience_ThrowsInvalidOperationException(string? audience)
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://issuer.example",
            ["JwtSettings:SecurityKey"] = "a-secret-key-that-is-at-least-thirty-two-characters",
            ["JwtSettings:Audience"] = audience,
        });

        // Act and assert
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwtAuthentication(configuration));
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
