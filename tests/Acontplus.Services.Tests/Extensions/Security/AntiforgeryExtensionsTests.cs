using Acontplus.Services.Extensions.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Acontplus.Services.Tests.Extensions.Security;

public sealed class AntiforgeryExtensionsTests
{
    [Fact]
    public void AddAcontplusAntiforgery_WithConfiguredHeader_RegistersAntiforgeryAndPreservesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddAcontplusAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(services, result);
        Assert.NotNull(provider.GetRequiredService<IAntiforgery>());
        Assert.Equal("X-CSRF-TOKEN", provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.HeaderName);
    }

    [Fact]
    public async Task MapAntiforgeryTokenEndpoint_WhenInvoked_IssuesRequestTokenAndCookieToken()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAcontplusAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        await using var app = builder.Build();
        app.MapAntiforgeryTokenEndpoint("/csrf/token");
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/csrf/token");
        var context = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        Assert.NotNull(endpoint.RequestDelegate);
        await endpoint.RequestDelegate(context);

        // Assert
        responseBody.Position = 0;
        using var response = await JsonDocument.ParseAsync(responseBody);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RootElement.GetProperty("requestToken").GetString()));
        Assert.Contains("Set-Cookie", context.Response.Headers.Keys);
    }
}
