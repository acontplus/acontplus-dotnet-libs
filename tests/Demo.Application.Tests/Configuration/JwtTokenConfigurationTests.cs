using Demo.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Demo.Application.Tests.Configuration;

public sealed class JwtTokenConfigurationTests
{
    [Fact]
    public void GetRequiredAudience_WithSingleAudience_ReturnsTrimmedAudience()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Audience"] = " https://api.example ",
        });

        // Act
        var audience = JwtTokenConfiguration.GetRequiredAudience(configuration);

        // Assert
        Assert.Equal("https://api.example", audience);
    }

    [Fact]
    public void GetRequiredAudience_WithMultipleAudiences_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Audience:0"] = "https://api-one.example",
            ["JwtSettings:Audience:1"] = "https://api-two.example",
        });

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => JwtTokenConfiguration.GetRequiredAudience(configuration));
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
