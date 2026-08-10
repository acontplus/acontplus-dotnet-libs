namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Represents a lookup item for dropdown lists, select boxes, and other UI components.
/// Supports hierarchical structures via ParentId and custom ordering via DisplayOrder.
/// All properties are nullable to support flexible SQL query mapping.
/// </summary>
public record LookupItem(
    int? Id,
    string? Code,
    string? Value,
    int? DisplayOrder,
    int? ParentId,
    bool? IsDefault,
    bool? IsActive,
    string? Description,
    string? Metadata);
