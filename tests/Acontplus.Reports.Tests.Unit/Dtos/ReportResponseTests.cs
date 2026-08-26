using Acontplus.Reports.Dtos;

namespace Acontplus.Reports.Tests.Unit.Dtos;

public sealed class ReportResponseTests
{
    [Fact]
    public void Dispose_WithPopulatedResponse_ClearsManagedReportDataAndIsIdempotent()
    {
        var response = new ReportResponse
        {
            FileContents = [1, 2, 3],
            ContentType = "application/pdf",
            FileDownloadName = "invoice.pdf"
        };

        response.Dispose();
        response.Dispose();

        Assert.Empty(response.FileContents);
        Assert.Equal(string.Empty, response.ContentType);
        Assert.Equal(string.Empty, response.FileDownloadName);
    }

    [Theory]
    [InlineData("#d61672", "#d61672")]
    [InlineData("#1E3A5F", "#2E86AB")]
    public void ThemeFactories_ReturnIndependentConfiguredThemes(string expectedHeader, string expectedAccent)
    {
        var theme = expectedHeader == "#d61672"
            ? QuestPdfColorThemes.AcontplusDefault()
            : QuestPdfColorThemes.Corporate();

        Assert.Equal(expectedHeader, theme.HeaderBackground);
        Assert.Equal(expectedAccent, theme.AccentColor);
        Assert.NotEqual(theme.HeaderBackground, theme.RowBackground);
    }
}
