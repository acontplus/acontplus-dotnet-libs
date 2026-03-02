namespace Acontplus.Reports.Dtos;

/// <summary>
/// Defines a merged band header cell rendered above the normal column header row in a
/// ClosedXML (advanced Excel) worksheet. Equivalent to the RDLC <c>ColSpan</c> idiom
/// used in reports like Kardex (Entradas / Salidas / Saldo grouped headers).
/// </summary>
public class AdvancedExcelGroupHeader
{
    /// <summary>Display text written into the merged band cell</summary>
    public required string Title { get; set; }

    /// <summary>
    /// 1-based index of the first column covered by this group header.
    /// Corresponds to the position in the visible column list, not the DataTable.
    /// </summary>
    public required int StartColumnIndex { get; set; }

    /// <summary>
    /// 1-based index of the last column covered by this group header (inclusive).
    /// Span = <see cref="EndColumnIndex"/> − <see cref="StartColumnIndex"/> + 1.
    /// </summary>
    public required int EndColumnIndex { get; set; }
}
