using Acontplus.Utilities.Mapping;
using Demo.Domain.Entities;

namespace Demo.Application.Mapping;

/// <summary>
/// Mapping profile for the Demo application.
/// Demonstrates all supported mapping scenarios:
/// <list type="bullet">
///   <item><description>Convention-based flat mapping (zero config)</description></item>
///   <item><description>Custom ForMember with expression and delegate resolvers</description></item>
///   <item><description>Nested complex-type mapping</description></item>
///   <item><description>Collection mapping</description></item>
///   <item><description>Constructor parameter mapping for immutable records</description></item>
///   <item><description>ReverseMap for bidirectional conversion</description></item>
///   <item><description>Ignore rules</description></item>
/// </list>
/// </summary>
public sealed class DemoMappingProfile : MappingProfile
{
    public DemoMappingProfile()
    {
        // 1. SIMPLE FLAT MAPPING — pure convention
        CreateMap<Order, OrderListItemDto>();

        // 2. NESTED OBJECT MAPPING — convention
        CreateMap<Order, OrderTimestampsDto>();

        // 3. COLLECTION ELEMENT MAPPING — convention
        CreateMap<OrderLineItem, OrderLineItemSummaryDto>();
    }
}
