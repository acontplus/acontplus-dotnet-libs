using Acontplus.Reports.Configuration;
using Acontplus.Reports.Dtos;
using Acontplus.Reports.Enums;
using Acontplus.Reports.Exceptions;
using Acontplus.Reports.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data;

namespace Acontplus.Reports.Tests.Unit.Services;

public sealed class CrossPlatformReportServicesTests
{
    [Fact]
    public async Task MiniExcelGenerateFromDataTableAsync_WithVisibleData_ReturnsXlsxResponse()
    {
        using var service = new MiniExcelReportService(
            NullLogger<MiniExcelReportService>.Instance,
            Options.Create(new ReportOptions()));

        using var response = await service.GenerateFromDataTableAsync(
            "sales",
            CreateSalesTable(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("sales.xlsx", response.FileDownloadName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.ContentType);
        Assert.True(response.FileContents.Length > 100);
        Assert.Equal((byte)'P', response.FileContents[0]);
        Assert.Equal((byte)'K', response.FileContents[1]);
    }

    [Fact]
    public async Task MiniExcelGenerateAsync_WithNoWorksheets_ThrowsContextualReportGenerationException()
    {
        using var service = new MiniExcelReportService(
            NullLogger<MiniExcelReportService>.Instance,
            Options.Create(new ReportOptions()));
        var request = new ExcelReportRequest { FileDownloadName = "empty" };

        var exception = await Assert.ThrowsAsync<ReportGenerationException>(() =>
            service.GenerateAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal("empty", exception.ReportPath);
        Assert.Equal("XLSX", exception.ReportFormat);
        Assert.Contains("worksheet", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClosedXmlGenerateFromDataTableAsync_WithTitleAndHeaders_ReturnsXlsxResponse()
    {
        using var service = new ClosedXmlReportService(
            NullLogger<ClosedXmlReportService>.Instance,
            Options.Create(new ReportOptions()));

        using var response = await service.GenerateFromDataTableAsync(
            "summary",
            CreateSalesTable(),
            worksheetName: "Sales",
            headerStyle: AdvancedExcelHeaderStyle.CorporateBlue(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("summary.xlsx", response.FileDownloadName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.ContentType);
        Assert.True(response.FileContents.Length > 100);
        Assert.Equal((byte)'P', response.FileContents[0]);
        Assert.Equal((byte)'K', response.FileContents[1]);
    }

    [Fact]
    public async Task QuestPdfGenerateAsync_WithTextSection_ReturnsPdfResponse()
    {
        using var service = new QuestPdfReportService(
            NullLogger<QuestPdfReportService>.Instance,
            Options.Create(new ReportOptions()));
        var request = new QuestPdfReportRequest
        {
            Title = "Monthly summary",
            FileDownloadName = "monthly-summary",
            Sections =
            [
                new QuestPdfSection
                {
                    Type = QuestPdfSectionType.Text,
                    TextBlocks = [new QuestPdfTextBlock { Content = "All systems operational." }]
                }
            ]
        };

        using var response = await service.GenerateAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("monthly-summary.pdf", response.FileDownloadName);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.True(response.FileContents.Length > 100);
        Assert.Equal("%PDF"u8.ToArray(), response.FileContents[..4]);
    }

    private static DataTable CreateSalesTable()
    {
        var table = new DataTable("Sales");
        table.Columns.Add("Customer", typeof(string));
        table.Columns.Add("Total", typeof(decimal));
        table.Rows.Add("Acontplus", 42.50m);
        table.Rows.Add("Example Corp", 99.99m);
        return table;
    }
}
