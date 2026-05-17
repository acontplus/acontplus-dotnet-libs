namespace Acontplus.Core.Enums;

/// <summary>Direction in which query results should be ordered.</summary>
public enum SortDirection
{
    /// <summary>No explicit sort direction; the data source decides.</summary>
    Default = 0,
    /// <summary>Ascending order (A → Z, 0 → 9).</summary>
    Asc = 1,
    /// <summary>Descending order (Z → A, 9 → 0).</summary>
    Desc = 2
}
