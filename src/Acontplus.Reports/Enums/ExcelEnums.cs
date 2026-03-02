namespace Acontplus.Reports.Enums;

/// <summary>Horizontal cell alignment for Excel output</summary>
public enum ExcelHorizontalAlignment
{
    /// <summary>General alignment (Excel default – numbers right, text left)</summary>
    General,

    /// <summary>Left-aligned text</summary>
    Left,

    /// <summary>Centred text</summary>
    Center,

    /// <summary>Right-aligned text (typical for numeric columns)</summary>
    Right
}

/// <summary>Aggregate function rendered in the totals row of an Excel worksheet</summary>
public enum ExcelAggregateType
{
    /// <summary>No aggregate (default)</summary>
    None,

    /// <summary>SUM formula</summary>
    Sum,

    /// <summary>AVERAGE formula</summary>
    Average,

    /// <summary>COUNT formula (counts numeric cells)</summary>
    Count,

    /// <summary>COUNTA formula (counts non-empty cells)</summary>
    CountA,

    /// <summary>MIN formula</summary>
    Min,

    /// <summary>MAX formula</summary>
    Max
}
