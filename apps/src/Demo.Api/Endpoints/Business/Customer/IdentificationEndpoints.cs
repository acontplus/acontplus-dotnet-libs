namespace Demo.Api.Endpoints.Business.Customer;

public static class IdentificationEndpoints
{
    public static void MapIdentificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer")
            .WithTags("Customer Identification");

        group.MapGet("/by-id-card", GetByIdCard)
            .WithName("GetIdCardInformation")
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetByIdCard(
        string idCard,
        [FromServices] ICustomerService customerService,
        [FromServices] ILogger<Program> logger,
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        // Get correlation ID from headers if needed
        var correlationId = httpContextAccessor.HttpContext?
            .Request.Headers["X-Correlation-Id"].FirstOrDefault();

        // Use the extension method directly
        return await customerService.GetByIdCardAsync(idCard)
            .ToGetMinimalApiResultAsync(correlationId);
    }
}
