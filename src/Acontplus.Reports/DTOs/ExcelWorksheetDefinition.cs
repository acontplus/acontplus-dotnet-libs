namespace Acontplus.Reports.Dtos;

/// <summary>
/// Defines a single worksheet within a simple MiniExcel workbook.
/// </summary>
public class ExcelWorksheetDefinition
{
    /// <summary>Worksheet tab name shown in Excel</summary>
    public required string Name { get; set; }

    /// <summary>Source data to write into this worksheet</summary>
    public required DataTable Data { get; set; }

    /// <summary>
    /// Optional column descriptors for header overrides, format hints, and column visibility.
    /// When <see langword="null"/>, all columns are exported using their original DataTable names.
    /// </summary>
    public List<ExcelColumnDefinition>? Columns { get; set; }

    /// <summary>When <see langword="true"/> a header row is rendered (default: <see langword="true"/>)</summary>
    public bool IncludeHeader { get; set; } = true;
}
