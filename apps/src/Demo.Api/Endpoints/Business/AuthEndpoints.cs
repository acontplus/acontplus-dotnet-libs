namespace Demo.Api.Endpoints.Business;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticates a user and returns a JWT token")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Registers a new user (auto-fills audit fields via HttpAuditContext)")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService auth,
        CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);

        return result.IsSuccess
            ? Results.Ok(ApiResponse<LoginResponse>.Success(result.Value))
            : Results.Json(
                ApiResponse.Failure([new ApiError(result.Error.Code, result.Error.Message)],
                    new ApiResponseOptions { StatusCode = System.Net.HttpStatusCode.Unauthorized }),
                statusCode: StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Register(
        LoginRequest request,
        IAuthService auth,
        CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);

        return result.IsSuccess
            ? Results.Created($"/api/usuario/{result.Value.UserId}",
                ApiResponse<LoginResponse>.Success(result.Value))
            : Results.Json(
                ApiResponse.Failure([new ApiError(result.Error.Code, result.Error.Message)]),
                statusCode: result.Error.Code == "USERNAME_EXISTS"
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest);
    }
}
