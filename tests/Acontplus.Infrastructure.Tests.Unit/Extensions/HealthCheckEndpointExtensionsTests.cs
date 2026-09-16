using Acontplus.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acontplus.Infrastructure.Tests.Unit.Extensions;

public class HealthCheckEndpointExtensionsTests
{
    [Fact]
    public void MapHealthCheckEndpoints_WithDefaultBasePath_RegistersHealthAndAliveEndpoints()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHealthChecks();
        var app = builder.Build();

        // Act
        app.MapHealthCheckEndpoints();

        // Assert
        var endpointSources = ((IEndpointRouteBuilder)app).DataSources;
        var endpoints = endpointSources.SelectMany(ds => ds.Endpoints).OfType<RouteEndpoint>().ToList();
        var routePatterns = endpoints.Select(e => e.RoutePattern.RawText).ToList();

        Assert.Contains("/health", routePatterns);
        Assert.Contains("/health/ready", routePatterns);
        Assert.Contains("/health/live", routePatterns);
        Assert.Contains("/alive", routePatterns);
    }
}
