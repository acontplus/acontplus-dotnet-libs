using Acontplus.Core.Dtos.Requests;
using Acontplus.Core.Enums;
using Acontplus.Core.Extensions;

namespace Acontplus.Core.Tests.Unit.Extensions;

public class FilterRequestExtensionsTests
{
    [Fact]
    public void WithSearch_WhenCalled_SetsSearchTerm()
    {
        var filter = new FilterRequest();
        var result = filter.WithSearch("test search");

        Assert.Equal("test search", result.SearchTerm);
    }

    [Fact]
    public void WithSort_WhenCalled_SetsSortByAndDirection()
    {
        var filter = new FilterRequest();
        var result = filter.WithSort("CreatedAt", SortDirection.Desc);

        Assert.Equal("CreatedAt", result.SortBy);
        Assert.Equal(SortDirection.Desc, result.SortDirection);
    }

    [Fact]
    public void WithFilters_WhenCalled_MergesFilters()
    {
        var filter = new FilterRequest().WithFilter("key1", "val1");
        var result = filter.WithFilters(new Dictionary<string, object> { { "key2", "val2" } });

        Assert.NotNull(result.Filters);
        Assert.Equal("val1", result.Filters["key1"]);
        Assert.Equal("val2", result.Filters["key2"]);
    }

    [Fact]
    public void WithFilter_WhenCalled_AddsSingleFilter()
    {
        var filter = new FilterRequest();
        var result = filter.WithFilter("status", "Active");

        Assert.NotNull(result.Filters);
        Assert.Equal("Active", result.Filters["status"]);
    }

    [Fact]
    public void GetFilterValue_WhenDictionaryIsNull_ReturnsDefaultValue()
    {
        IReadOnlyDictionary<string, object>? dict = null;

        var result = dict.GetFilterValue("status", "default");

        Assert.Equal("default", result);
    }

    [Fact]
    public void GetFilterValue_WhenKeyNotFound_ReturnsDefaultValue()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object> { { "otherKey", "value" } }
        };

        var result = request.GetFilterValue("status", "default");

        Assert.Equal("default", result);
    }

    [Fact]
    public void GetFilterValue_WhenKeyExists_ReturnsConvertedValue()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object> { { "limit", 50 } }
        };

        var result = request.GetFilterValue("limit", 10);

        Assert.Equal(50, result);
    }

    [Fact]
    public void GetFilterValue_FromStringToInt_ReturnsConvertedValue()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object> { { "count", "42" } }
        };

        var result = request.GetFilterValue("count", 0);

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetFilterValue_WithoutDefault_WhenKeyExists_ReturnsValue()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object> { { "name", "John" } }
        };

        var result = request.GetFilterValue<string>("name");

        Assert.Equal("John", result);
    }

    [Fact]
    public void GetFilterValue_WithoutDefault_WhenKeyMissing_ReturnsTypeDefault()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object>()
        };

        var result = request.GetFilterValue<int>("missing");

        Assert.Equal(0, result);
    }

    [Fact]
    public void TryGetFilterValue_WhenKeyExists_ReturnsTrueAndOutValue()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object> { { "isActive", true } }
        };

        var success = request.TryGetFilterValue<bool>("isActive", out var value);

        Assert.True(success);
        Assert.True(value);
    }

    [Fact]
    public void TryGetFilterValue_WhenKeyMissing_ReturnsFalseAndDefault()
    {
        var request = new FilterRequest
        {
            Filters = new Dictionary<string, object>()
        };

        var success = request.TryGetFilterValue<int>("missing", out var value);

        Assert.False(success);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetFilterValue_OnDictionary_WhenKeyExists_ReturnsTrue()
    {
        IReadOnlyDictionary<string, object> dict = new Dictionary<string, object>
        {
            { "total", 100 }
        };

        var success = dict.TryGetFilterValue<int>("total", out var value);

        Assert.True(success);
        Assert.Equal(100, value);
    }

    [Fact]
    public void TryGetFilterValue_OnDictionary_WhenDictionaryNull_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object>? dict = null;

        var success = dict.TryGetFilterValue<int>("total", out var value);

        Assert.False(success);
        Assert.Equal(0, value);
    }
}
