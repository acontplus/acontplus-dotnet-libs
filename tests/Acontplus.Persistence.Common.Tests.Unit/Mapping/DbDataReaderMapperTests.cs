using System.Data;
using Acontplus.Persistence.Common.Mapping;

namespace Acontplus.Persistence.Common.Tests.Unit.Mapping;

public class DbDataReaderMapperTests
{
    private sealed class SampleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    [Fact]
    public async Task ToListAsync_WhenReaderHasRows_MapsPropertiesCorrectly()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));

        table.Rows.Add(1, "Alpha", 100.50m);
        table.Rows.Add(2, "Beta", 200.75m);

        using var reader = table.CreateDataReader();

        var result = await reader.ToListAsync<SampleDto>(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal(100.50m, result[0].Amount);

        Assert.Equal(2, result[1].Id);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal(200.75m, result[1].Amount);
    }

    [Fact]
    public async Task ToListAsync_WhenReaderEmpty_ReturnsEmptyList()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));

        using var reader = table.CreateDataReader();

        var result = await reader.ToListAsync<SampleDto>(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ToListAsync_WhenNullValuesPresent_AssignsDefaultValues()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));

        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);

        using var reader = table.CreateDataReader();

        var result = await reader.ToListAsync<SampleDto>(TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(0, result[0].Id);
        Assert.Equal(0m, result[0].Amount);
    }
}
