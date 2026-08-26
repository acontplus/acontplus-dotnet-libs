using Acontplus.Reports.Configuration;
using Acontplus.Reports.Extensions;
using Acontplus.Reports.Interfaces;
using Acontplus.Reports.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Acontplus.Reports.Tests.Unit.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddReportServices_WithProgrammaticOptions_RegistersCrossPlatformContractsAndNormalizesCacheLimits()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddReportServices(options =>
        {
            options.MaxCachedReportDefinitions = 0;
            options.CacheTtlMinutes = 0;
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = provider.GetRequiredService<IOptions<ReportOptions>>().Value;

        Assert.Same(services, result);
        Assert.IsType<MiniExcelReportService>(scope.ServiceProvider.GetRequiredService<IMiniExcelReportService>());
        Assert.IsType<ClosedXmlReportService>(scope.ServiceProvider.GetRequiredService<IClosedXmlReportService>());
        Assert.IsType<QuestPdfReportService>(scope.ServiceProvider.GetRequiredService<IQuestPdfReportService>());
        Assert.NotNull(provider.GetRequiredService<ReportDefinitionCache>());
        Assert.Equal(0, options.MaxCachedReportDefinitions);
        Assert.Equal(0, options.CacheTtlMinutes);
    }
}
