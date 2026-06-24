namespace Demo.Application.Dtos;

// ═══════════════════════════════════════════════════════════════════
// SIMPLE FLAT DTO MAPPING (convention-based, zero configuration)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Simple flat DTO for order list views — maps by convention from <see cref="Demo.Domain.Entities.Order"/>.
/// </summary>
public class OrderListItemDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
// COMPLEX NESTED DTO (nested object + custom member rules)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Detailed order view with nested customer info, line items, and computed fields.
/// Demonstrates nested object mapping, collection mapping, and ForMember rules.
/// </summary>
public class OrderDetailDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderTimestampsDto Timestamps { get; set; } = new();
    public List<OrderLineItemSummaryDto> LineItems { get; set; } = [];
}

/// <summary>
/// Nested DTO for order timestamps — demonstrates nested object mapping.
/// </summary>
public class OrderTimestampsDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}

/// <summary>
/// Line item summary — demonstrates collection element mapping.
/// </summary>
public class OrderLineItemSummaryDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
// IMMUTABLE RECORD (constructor parameter mapping)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Immutable order summary record — demonstrates constructor parameter mapping
/// for types with no parameterless constructor.
/// </summary>
public record OrderSummaryRecord(
    int Id,
    string CustomerName,
    decimal TotalAmount,
    string Status);

// ═══════════════════════════════════════════════════════════════════
// REVERSE MAP DEMO (bidirectional mapping)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// User profile DTO — demonstrates ReverseMap() for bidirectional mapping.
/// </summary>
public class UserProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
// RESPONSE WRAPPERS
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Aggregated response for the mapping demo endpoint showing all mapping scenarios.
/// </summary>
public class MappingDemoResponse
{
    /// <summary>Convention-based flat mapping result (Order → OrderListItemDto).</summary>
    public List<OrderListItemDto> FlatMappedOrders { get; set; } = [];

    /// <summary>Complex nested mapping result (Order + LineItems → OrderDetailDto).</summary>
    public OrderDetailDto? DetailedOrder { get; set; }

    /// <summary>Constructor-mapped immutable record (Order → OrderSummaryRecord).</summary>
    public OrderSummaryRecord? ImmutableRecord { get; set; }

    /// <summary>Collection mapping result (IEnumerable&lt;Order&gt; → IEnumerable&lt;OrderListItemDto&gt;).</summary>
    public int CollectionMappedCount { get; set; }

    /// <summary>Bidirectional mapping: DTO → Entity → DTO round-trip.</summary>
    public UserProfileDto? RoundTrippedProfile { get; set; }
}
