using Acontplus.Reports.Configuration;
using Acontplus.Reports.Dtos;

namespace Acontplus.Reports.Tests.Unit.Configuration;

public sealed class ReportOptionsTests
{
    [Fact]
    public void Constructor_WithDefaults_UsesSafeLimitsAndSupportedExtensions()
    {
        var options = new ReportOptions();

        Assert.Equal("Reports", options.MainDirectory);
        Assert.Equal(100 * 1024 * 1024, options.MaxReportSizeBytes);
        Assert.Equal(300, options.ReportGenerationTimeoutSeconds);
        Assert.Equal(10, options.MaxConcurrentReports);
        Assert.Equal(QuestPdfLicenseType.Community, options.QuestPdfLicenseType);
        Assert.Equal([".rdlc", ".rdl"], options.AllowedReportExtensions);
    }
}
