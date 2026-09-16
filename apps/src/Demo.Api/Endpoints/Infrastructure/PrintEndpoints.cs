namespace Demo.Api.Endpoints.Infrastructure;

public static class PrintEndpoints
{
    public static void MapPrintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/print")
            .WithTags("Print");

        group.MapGet("/", () => Results.Ok());
    }
}
