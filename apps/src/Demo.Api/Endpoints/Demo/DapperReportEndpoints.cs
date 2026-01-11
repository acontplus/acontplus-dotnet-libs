using Acontplus.Core.Dtos.Requests;
using Acontplus.Core.Enums;
using Acontplus.Utilities.Dtos;
using Demo.Application.Dtos;
using Demo.Application.Interfaces;

namespace Demo.Api.Endpoints.Demo;

/// <summary>
/// Demonstrates Dapper-based repository for high-performance read operations.
/// This endpoint group showcases the new IDapperRepository feature (v2.2.0).
/// </summary>
public static class DapperReportEndpoints
{
    public static RouteGroupBuilder MapDapperReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/paged", GetPagedOrders)
            .WithName("GetPagedOrders")
            .WithDescription("Gets paginated orders using Dapper for optimal performance");

        group.MapGet("/summary", GetOrderSummary)
            .WithName("GetOrderSummary")
            .WithDescription("Gets order summary statistics using Dapper");

        group.MapGet("/by-status/{status}", GetOrdersByStatus)
            .WithName("GetOrdersByStatus")
            .WithDescription("Gets orders by status using Dapper with parameterized query");

        group.MapGet("/dashboard", GetDashboard)
            .WithName("GetOrderDashboard")
            .WithDescription("Gets dashboard data using Dapper QueryMultiple for efficient multi-result queries");

        return group;
    }

    /// <summary>
    /// Gets paginated orders using Dapper.
    /// Demonstrates: GetPagedAsync with automatic OFFSET-FETCH pagination.
    /// </summary>
    private static async Task<IResult> GetPagedOrders(
        PaginationQuery pagination,
        IOrderReportService reportService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // Map PaginationQuery to PaginationRequest
        var request = new PaginationRequest
        {
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            SortBy = pagination.SortBy,
            SortDirection = pagination.SortDirection ?? SortDirection.Asc
        };

        logger.LogInformation("Dapper: Getting paged orders - Page {Page}, Size {Size}",
            request.PageIndex, request.PageSize);

        var result = await reportService.GetPagedOrdersAsync(request, ct);

        return Results.Ok(result);
    }

    /// <summary>
    /// Gets order summary statistics using Dapper.
    /// Demonstrates: QueryFirstOrDefaultAsync for single-row aggregate queries.
    /// </summary>
    private static async Task<IResult> GetOrderSummary(
        IOrderReportService reportService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Dapper: Getting order summary statistics");

        var summary = await reportService.GetOrderSummaryAsync(ct);

        return Results.Ok(summary);
    }

    /// <summary>
    /// Gets orders filtered by status using Dapper.
    /// Demonstrates: QueryAsync with parameterized SQL (safe from SQL injection).
    /// </summary>
    private static async Task<IResult> GetOrdersByStatus(
        string status,
        IOrderReportService reportService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Dapper: Getting orders by status {Status}", status);

        var orders = await reportService.GetOrdersByStatusAsync(status, ct);

        return Results.Ok(orders);
    }

    /// <summary>
    /// Gets dashboard data using Dapper QueryMultiple.
    /// Demonstrates: QueryMultipleAsync for fetching multiple result sets in a single round-trip.
    /// This is significantly more efficient than making 3 separate database calls.
    /// </summary>
    private static async Task<IResult> GetDashboard(
        IOrderReportService reportService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Dapper: Getting order dashboard with multi-query");

        var dashboard = await reportService.GetDashboardAsync(ct);

        return Results.Ok(dashboard);
    }
}
