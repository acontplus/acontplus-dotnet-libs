using System.ComponentModel.DataAnnotations;

namespace Acontplus.Core.Dtos.Requests;

/// <summary>
/// Configuration options for database command execution.
/// </summary>
public class CommandOptionsDto
{
    /// <summary>
    /// Gets or sets the command timeout in seconds. 
    /// 0 indicates no timeout. Null means use ADO.NET default (usually 30 seconds).
    /// </summary>
    [Range(0, 3600)]
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets how the command text is to be interpreted (StoredProcedure or Text).
    /// </summary>
    public CommandType CommandType { get; set; } = CommandType.StoredProcedure;

    /// <summary>
    /// Gets or sets a value indicating whether to include table names in the result.
    /// Specific to GetDataSetAsync.
    /// </summary>
    public bool WithTableNames { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum length for table names.
    /// </summary>
    [Range(1, 1000)]
    public int TableNamesLength { get; set; } = 500;

    /// <summary>
    /// Controls filter parameter strategy:
    /// - false (default): Individual parameters for raw SQL (better performance)
    /// - true: JSON serialized parameters for stored procedures (flexibility)
    /// - null: Auto-detect based on CommandType (JSON for StoredProcedure)
    /// </summary>
    public bool? UseJsonFilters { get; set; }
}

