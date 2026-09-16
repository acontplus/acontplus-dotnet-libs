using Acontplus.Core.Abstractions.Infrastructure.Caching;
using Acontplus.Infrastructure.Caching;
using Acontplus.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acontplus.Infrastructure.Tests.Unit.Extensions;

public class InfrastructureCachingExtensionsTests
{
    [Fact]
    public void AddCachingServices_WithAspireCacheConnectionString_RegistersDistributedCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:cache"] = "localhost:6379"
            })
            .Build();

        // Act
        services.AddCachingServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<ICacheService>();
        Assert.NotNull(cacheService);
        Assert.IsType<DistributedCacheService>(cacheService);
    }

    [Fact]
    public void AddCachingServices_WithoutRedisConnectionString_RegistersMemoryCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddCachingServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<ICacheService>();
        Assert.NotNull(cacheService);
        Assert.IsType<MemoryCacheService>(cacheService);
    }
}
