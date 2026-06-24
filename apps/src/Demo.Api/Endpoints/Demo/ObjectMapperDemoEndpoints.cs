using Acontplus.Utilities.Mapping;
using Demo.Domain.Entities;

namespace Demo.Api.Endpoints.Demo;

/// <summary>
/// Demonstrates the redesigned ObjectMapper with compiled-delegate mapping.
/// Showcases: flat convention, nested objects, collections, constructor mapping,
/// delegate resolvers, ignore rules, and reverse maps.
/// </summary>
public static class ObjectMapperDemoEndpoints
{
    public static void MapObjectMapperDemoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/demo/object-mapper")
            .WithTags("Object Mapper Demo");

        group.MapGet("/all-scenarios", DemoAllScenarios)
            .WithName("DemoAllMappingScenarios")
            .WithSummary("Demonstrates all mapping scenarios: flat, nested, collection, constructor, and reverse")
            .Produces<ApiResponse<MappingDemoResponse>>(StatusCodes.Status200OK);

        group.MapGet("/flat/{id:int}", DemoFlatMapping)
            .WithName("DemoFlatMapping")
            .WithSummary("Convention-based flat mapping: Order → OrderListItemDto")
            .Produces<ApiResponse<OrderListItemDto>>(StatusCodes.Status200OK);

        group.MapGet("/nested/{id:int}", DemoNestedMapping)
            .WithName("DemoNestedMapping")
            .WithSummary("Nested + collection mapping: Order → OrderDetailDto with timestamps and line items")
            .Produces<ApiResponse<OrderDetailDto>>(StatusCodes.Status200OK);

        group.MapGet("/record/{id:int}", DemoConstructorMapping)
            .WithName("DemoConstructorMapping")
            .WithSummary("Constructor parameter mapping: Order → OrderSummaryRecord (immutable)")
            .Produces<ApiResponse<OrderSummaryRecord>>(StatusCodes.Status200OK);

        group.MapPost("/round-trip", DemoRoundTrip)
            .WithName("DemoRoundTrip")
            .WithSummary("Entity-to-DTO mapping: Usuario → UserProfileDto")
            .Produces<ApiResponse<UserProfileDto>>(StatusCodes.Status200OK);

        group.MapGet("/collection", DemoCollectionMapping)
            .WithName("DemoCollectionMapping")
            .WithSummary("Collection mapping: IEnumerable<Order> → IEnumerable<OrderListItemDto>")
            .Produces<ApiResponse<IEnumerable<OrderListItemDto>>>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Runs all mapping scenarios in a single call — useful for verifying the
    /// compiled-delegate mapper works end-to-end.
    /// </summary>
    private static IResult DemoAllScenarios(IObjectMapper mapper)
    {
        var sampleOrders = CreateSampleOrders();
        var sampleLineItems = CreateSampleLineItems(sampleOrders[0].Id);
        var firstOrder = sampleOrders[0];

        // 1. Flat convention mapping (zero config)
        var flatMapped = mapper.Map<Order, OrderListItemDto>(firstOrder);

        // 2. Complex mapping with ForMember delegate + manual nested assignment
        var detail = mapper.Map<Order, OrderDetailDto>(firstOrder);
        detail.Timestamps = mapper.Map<Order, OrderTimestampsDto>(firstOrder);
        detail.LineItems = sampleLineItems
            .Select(li => mapper.Map<OrderLineItem, OrderLineItemSummaryDto>(li))
            .ToList();

        // 3. Constructor-mapped immutable record
        var record = mapper.Map<Order, OrderSummaryRecord>(firstOrder);

        // 4. Collection mapping (maps all elements via compiled delegate)
        var collectionMapped = mapper.Map<Order, OrderListItemDto>(sampleOrders).ToList();

        // 5. Forward map: Entity → DTO
        var sampleUser = new Usuario
        {
            Id = 42,
            Username = "demo_user",
            Email = "demo@acontplus.com",
            RoleId = 2,
            CreatedAt = DateTime.UtcNow
        };
        var roundTripped = mapper.Map<Usuario, UserProfileDto>(sampleUser);

        var response = new MappingDemoResponse
        {
            FlatMappedOrders = [flatMapped],
            DetailedOrder = detail,
            ImmutableRecord = record,
            CollectionMappedCount = collectionMapped.Count,
            RoundTrippedProfile = roundTripped
        };

        return Results.Ok(ApiResponse.Success(response));
    }

    private static IResult DemoFlatMapping(int id, IObjectMapper mapper)
    {
        var order = CreateSampleOrders().FirstOrDefault(o => o.Id == id)
                    ?? CreateSampleOrders()[0];

        var dto = mapper.Map<Order, OrderListItemDto>(order);
        return Results.Ok(ApiResponse.Success(dto));
    }

    private static IResult DemoNestedMapping(int id, IObjectMapper mapper)
    {
        var order = CreateSampleOrders().FirstOrDefault(o => o.Id == id)
                    ?? CreateSampleOrders()[0];
        var lineItems = CreateSampleLineItems(order.Id);

        // Map the order detail (ForMember resolves StatusDisplay via delegate)
        var detail = mapper.Map<Order, OrderDetailDto>(order);

        // Map nested timestamps (convention-based)
        detail.Timestamps = mapper.Map<Order, OrderTimestampsDto>(order);

        // Map collection of line items
        detail.LineItems = mapper.Map<OrderLineItem, OrderLineItemSummaryDto>(lineItems).ToList();

        return Results.Ok(ApiResponse.Success(detail));
    }

    private static IResult DemoConstructorMapping(int id, IObjectMapper mapper)
    {
        var order = CreateSampleOrders().FirstOrDefault(o => o.Id == id)
                    ?? CreateSampleOrders()[0];

        // Maps to immutable record via constructor parameters
        var record = mapper.Map<Order, OrderSummaryRecord>(order);
        return Results.Ok(ApiResponse.Success(record));
    }

    private static IResult DemoRoundTrip(UserProfileDto input, IObjectMapper mapper)
    {
        // Create entity from the input data
        var entity = new Usuario
        {
            Id = 1,
            Username = input.Username,
            Email = input.Email,
            RoleId = input.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        // Map entity → DTO using compiled delegate
        var output = mapper.Map<Usuario, UserProfileDto>(entity);

        return Results.Ok(ApiResponse.Success(output));
    }

    private static IResult DemoCollectionMapping(IObjectMapper mapper)
    {
        var orders = CreateSampleOrders();

        // Single call maps the entire collection via the compiled element delegate
        var dtos = mapper.Map<Order, OrderListItemDto>(orders);

        return Results.Ok(ApiResponse.Success(dtos));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sample data factories (in a real app these come from the repository)
    // ─────────────────────────────────────────────────────────────────────

    private static List<Order> CreateSampleOrders() =>
    [
        new()
        {
            Id = 1,
            CustomerName = "Acontplus Corp",
            ProductName = "Enterprise License",
            Quantity = 5,
            Price = 299.99m,
            TotalAmount = 1499.95m,
            Status = OrderStatus.Processing,
            CreatedAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            ProcessedAt = new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 2,
            CustomerName = "TechStart Inc",
            ProductName = "Starter Pack",
            Quantity = 1,
            Price = 49.99m,
            TotalAmount = 49.99m,
            Status = OrderStatus.Created,
            CreatedAt = new DateTime(2026, 2, 20, 14, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 3,
            CustomerName = "Global Solutions",
            ProductName = "Premium Support",
            Quantity = 10,
            Price = 99.00m,
            TotalAmount = 990.00m,
            Status = OrderStatus.Shipped,
            CreatedAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            ProcessedAt = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
            ShippedAt = new DateTime(2026, 3, 3, 16, 30, 0, DateTimeKind.Utc),
            TrackingNumber = "TRACK-2026-0301"
        }
    ];

    private static List<OrderLineItem> CreateSampleLineItems(int orderId) =>
    [
        new()
        {
            Id = 101,
            OrderId = orderId,
            ProductName = "Enterprise License",
            Quantity = 3,
            UnitPrice = 299.99m,
            LineTotal = 899.97m
        },
        new()
        {
            Id = 102,
            OrderId = orderId,
            ProductName = "Priority Support Add-on",
            Quantity = 2,
            UnitPrice = 149.99m,
            LineTotal = 299.98m
        }
    ];
}
