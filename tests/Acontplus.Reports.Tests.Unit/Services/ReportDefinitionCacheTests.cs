using Acontplus.Reports.Services;

namespace Acontplus.Reports.Tests.Unit.Services;

public sealed class ReportDefinitionCacheTests
{
    [Fact]
    public async Task GetOrAddAsync_WithSameKey_ReturnsIndependentCopiesAndInvokesFactoryOnce()
    {
        using var cache = new ReportDefinitionCache(10, TimeSpan.FromMinutes(1));
        var factoryCalls = 0;

        Task<MemoryStream> Factory(string _) => Task.FromResult(new MemoryStream([1, 2, 3]));

        using var first = await cache.GetOrAddAsync("invoice", key =>
        {
            factoryCalls++;
            return Factory(key);
        });
        first.WriteByte(4);
        using var second = await cache.GetOrAddAsync("invoice", Factory);

        Assert.Equal(1, factoryCalls);
        Assert.NotSame(first, second);
        Assert.Equal([1, 2, 3], second.ToArray());
    }

    [Fact]
    public async Task Clear_AfterCachedEntry_InvokesFactoryForNextRequest()
    {
        using var cache = new ReportDefinitionCache(10, TimeSpan.FromMinutes(1));
        var factoryCalls = 0;

        Task<MemoryStream> Factory(string _) => Task.FromResult(new MemoryStream([7]));

        using var first = await cache.GetOrAddAsync("summary", key =>
        {
            factoryCalls++;
            return Factory(key);
        });
        cache.Clear();
        using var second = await cache.GetOrAddAsync("summary", key =>
        {
            factoryCalls++;
            return Factory(key);
        });

        Assert.Equal(2, factoryCalls);
        Assert.Equal([7], second.ToArray());
    }
}
