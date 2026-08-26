using Acontplus.Reports.Helpers;

namespace Acontplus.Reports.Tests.Unit.Helpers;

public sealed class RdlcHelpersTests
{
    [Fact]
    public void LoadReportDefinition_WithExistingFile_ReturnsReadableStreamAtBeginning()
    {
        var path = Path.GetTempFileName();
        var expected = "<Report>invoice</Report>"u8.ToArray();
        File.WriteAllBytes(path, expected);

        try
        {
            using var stream = RdlcHelpers.LoadReportDefinition(path);

            Assert.Equal(0, stream.Position);
            Assert.Equal(expected, stream.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
