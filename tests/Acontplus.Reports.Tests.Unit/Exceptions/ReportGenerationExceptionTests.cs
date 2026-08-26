using Acontplus.Reports.Exceptions;
using System.Security;

namespace Acontplus.Reports.Tests.Unit.Exceptions;

public sealed class ReportGenerationExceptionTests
{
    [Fact]
    public void Constructor_WithReportContextAndInnerException_PreservesAllContext()
    {
        var innerException = new InvalidOperationException("storage unavailable");

        var exception = new ReportGenerationException("Could not generate report", "reports/invoice.rdlc", "PDF", innerException);

        Assert.Equal("reports/invoice.rdlc", exception.ReportPath);
        Assert.Equal("PDF", exception.ReportFormat);
        Assert.Same(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(1024L, 512L)]
    [InlineData(104857601L, 104857600L)]
    public void ReportSizeExceededException_WithExceededLimit_ExposesSizesInMessage(long reportSize, long maxSize)
    {
        var exception = new ReportSizeExceededException(reportSize, maxSize);

        Assert.Equal(reportSize, exception.ReportSize);
        Assert.Equal(maxSize, exception.MaxSize);
        Assert.Contains(reportSize.ToString(), exception.Message);
        Assert.Contains(maxSize.ToString(), exception.Message);
    }

    [Fact]
    public void FromSecurityException_WithReportPath_PreservesInnerExceptionAndPath()
    {
        var securityException = new SecurityException("Directory traversal is not allowed.");

        var exception = InvalidReportPathException.FromSecurityException(securityException, "../invoice.rdlc");

        Assert.Same(securityException, exception.InnerException);
        Assert.Contains("../invoice.rdlc", exception.Message);
        Assert.Contains("Directory traversal", exception.Message);
    }
}
