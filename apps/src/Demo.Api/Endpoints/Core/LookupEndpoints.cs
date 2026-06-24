namespace Demo.Api.Endpoints.Core;

/// <summary>
/// Minimal API endpoints for lookup/reference data management.
/// Demonstrates the LookupService with caching capabilities.
/// </summary>
public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lookups")
            .WithDescription("Endpoints for managing test API lookups.")
            .WithTags("Lookups");

        group.MapGet("/", async (
            FilterQuery filterQuery,
            ILookupService lookupService,
            CancellationToken cancellationToken) =>
        {
            var filterRequest = filterQuery.ToFilterRequest();

            return await lookupService
                .GetLookupsAsync("dbo.GetLookups", filterRequest, cancellationToken)
                .ToGetMinimalApiResultAsync();
        })
        .WithName("GetLookups")
        .WithSummary("Get test API lookups")
        .WithDescription("Retrieve test API lookups based on filters with automatic caching (30-minute TTL). Supports filters: category, isActive, minPriority, entityId.")
        .Produces<IDictionary<string, IEnumerable<LookupItem>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/refresh", async (
            FilterQuery filterQuery,
            ILookupService lookupService,
            CancellationToken cancellationToken) =>
        {
            var filterRequest = filterQuery.ToFilterRequest();

            return await lookupService
                .RefreshLookupsAsync("dbo.GetLookups", filterRequest, cancellationToken)
                .ToGetMinimalApiResultAsync();
        })
        .WithName("RefreshLookups")
        .WithSummary("Refresh test API lookups cache")
        .WithDescription("Forces a cache refresh for test API lookups based on filters. Supports filters: forceRefresh, cacheKey.")
        .Produces<IDictionary<string, IEnumerable<LookupItem>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
