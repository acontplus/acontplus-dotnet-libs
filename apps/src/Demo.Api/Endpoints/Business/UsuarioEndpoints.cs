using System.Security.Claims;

namespace Demo.Api.Endpoints.Business;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuario")
            .WithTags("Usuario");

        group.MapGet("/{id:int}", GetUsuario)
            .WithName("GetUsuario")
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("", CreateUsuario)
            .WithName("CreateUsuario")
            .Produces<ApiResponse<Usuario>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id:int}", UpdateUsuario)
            .WithName("UpdateUsuario")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id:int}", DeleteUsuario)
            .WithName("DeleteUsuario")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetUsuarios)
            .WithName("GetUsuarios")
            .Produces<ApiResponse<PagedResult<UsuarioDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/import", ImportUsuarios)
            .WithName("ImportUsuarios")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/legacy-ado", GetUsersAdo)
            .WithName("GetUsersAdo")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/get-dynamic", GetDynamicUsers)
            .WithName("GetDynamicUsers")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // High-Performance ADO.NET Endpoints
        group.MapGet("/count", GetUserCount)
            .WithName("GetUserCount")
            .WithSummary("Gets the total count of users using scalar query")
            .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/exists/{username}", CheckUserExists)
            .WithName("CheckUserExists")
            .WithSummary("Checks if a user exists by username")
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/active-count", GetActiveUsersCount)
            .WithName("GetActiveUsersCount")
            .WithSummary("Gets count of active users (modified in last 6 months)")
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/paged-ado", GetPagedUsersAdo)
            .WithName("GetPagedUsersAdo")
            .WithSummary("Gets paginated users using high-performance ADO.NET with optional search")
            .Produces<ApiResponse<PagedResult<UsuarioDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/paged-complex", GetPagedUsersComplex)
            .WithName("GetPagedUsersComplex")
            .WithSummary("Gets paginated users with complex filtering (created in last 30 days)")
            .Produces<ApiResponse<PagedResult<UsuarioDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/paged-sp", GetPagedUsersFromStoredProc)
            .WithName("GetPagedUsersFromStoredProc")
            .WithSummary("Gets paginated users using stored procedure with JSON filters")
            .Produces<ApiResponse<PagedResult<UsuarioDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/bulk", BulkInsertUsers)
            .WithName("BulkInsertUsers")
            .WithSummary("Bulk inserts users using SqlBulkCopy for maximum performance")
            .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/batch", ExecuteBatchOperations)
            .WithName("ExecuteBatchOperations")
            .WithSummary("Executes batch operations (updates) in a single transaction")
            .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Test endpoints for exception handling
        group.MapGet("/test-exception/{id:int}", GetUserWithException)
            .WithName("GetUserWithException")
            .WithSummary("Test endpoint that triggers an exception in the service layer")
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/test-custom-error/{id:int}", GetUserWithCustomError)
            .WithName("GetUserWithCustomError")
            .WithSummary("Test endpoint that returns a custom domain error")
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetUsuario(int id, IUsuarioService usuarioService)
    {
        return await usuarioService.GetByIdAsync(id).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> CreateUsuario(
        UsuarioDto usuarioDto,
        IUsuarioService usuarioService)
    {
        var usuario = ObjectMapper.Map<UsuarioDto, Usuario>(usuarioDto);
        if (usuario == null)
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("MAPPING_ERROR", "Invalid user data")]));

        var result = await usuarioService.AddAsync(usuario);

        return result.Match<IResult>(
            value =>
            {
                if (value is not null)
                {
                    var response = ApiResponse.Success(value);
                    return Results.Created($"/api/usuario/{value.Id}", response);
                }
                var successResponse = ApiResponse.Success(value, new ApiResponseOptions { Message = "Usuario creado exitosamente." });
                return Results.Ok(successResponse);
            },
            error => error.ToApiResponse<Usuario>().ToMinimalApiResult()
        );
    }

    private static async Task<IResult> UpdateUsuario(
        int id,
        UsuarioDto usuarioDto,
        IUsuarioService usuarioService)
    {
        var usuario = ObjectMapper.Map<UsuarioDto, Usuario>(usuarioDto);
        if (usuario == null)
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("MAPPING_ERROR", "Invalid user data")]));

        return await usuarioService.UpdateAsync(id, usuario).ToMinimalApiResultAsync();
    }

    private static async Task<IResult> DeleteUsuario(int id, IUsuarioService usuarioService)
    {
        return await usuarioService.DeleteAsync(id).ToMinimalApiResultAsync();
    }

    private static async Task<IResult> GetUsuarios(
        PaginationQuery pagination,
        IUsuarioService usuarioService)
    {
        // Map PaginationQuery to PaginationRequest
        var PaginationRequest = new PaginationRequest
        {
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            SortBy = pagination.SortBy,
            SortDirection = pagination.SortDirection ?? SortDirection.Asc,
            SearchTerm = pagination.SearchTerm,
            Filters = pagination.Filters
        };

        return await usuarioService.GetPaginatedUsersAsync(PaginationRequest).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> ImportUsuarios(
        List<UsuarioDto> dtos,
        IUsuarioService usuarioService)
    {
        return await usuarioService.ImportUsuariosAsync(dtos).ToMinimalApiResultAsync();
    }

    private static async Task<IResult> GetUsersAdo(IUsuarioService usuarioService)
    {
        return await usuarioService.GetLegacySpResponseAsync().ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetDynamicUsers(IUsuarioService usuarioService)
    {
        return await usuarioService.GetDynamicUserListAsync().ToGetMinimalApiResultAsync();
    }

    #region High-Performance ADO.NET Endpoints

    private static async Task<IResult> GetUserCount(IUsuarioService usuarioService)
    {
        return await usuarioService.GetUserCountAsync().ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> CheckUserExists(string username, IUsuarioService usuarioService)
    {
        return await usuarioService.CheckUserExistsAsync(username).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetActiveUsersCount(IUsuarioService usuarioService)
    {
        return await usuarioService.GetActiveUsersCountAsync().ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetPagedUsersAdo(
        PaginationQuery pagination,
        IUsuarioService usuarioService)
    {
        // Map PaginationQuery to PaginationRequest
        var PaginationRequest = new PaginationRequest
        {
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            SortBy = pagination.SortBy,
            SortDirection = pagination.SortDirection ?? SortDirection.Asc,
            SearchTerm = pagination.SearchTerm,
            Filters = pagination.Filters
        };

        // ✅ Example: Use GetFilterValue to extract and validate filter values from original pagination
        var showDeleted = PaginationRequest.GetFilterValue<bool>("showDeleted", false);
        var minAge = PaginationRequest.GetFilterValue<int>("minAge", 0);
        var role = PaginationRequest.GetFilterValue<string>("role", "User");

        // ✅ Example: Use TryGetFilterValue for conditional logic
        if (PaginationRequest.TryGetFilterValue<DateTime>("createdAfter", out var createdAfter))
        {
            // Additional validation or processing for date filter
            PaginationRequest = PaginationRequest.WithFilter("CreatedAfter", createdAfter);
        }

        // ✅ Example: Add extra filters using WithFilter extension
        if (!showDeleted)
        {
            PaginationRequest = PaginationRequest.WithFilter("IsDeleted", false);
        }

        if (role != null && role != "User")
        {
            PaginationRequest = PaginationRequest
                .WithFilter("Status", "Active")
                .WithFilter("MinAge", minAge)
                .WithFilter("Role", role);
        }
        else
        {
            PaginationRequest = PaginationRequest
                .WithFilter("Status", "Active")
                .WithFilter("MinAge", minAge);
        }

        return await usuarioService.GetPagedUsersAdoAsync(PaginationRequest).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetPagedUsersComplex(
        PaginationQuery pagination,
        IUsuarioService usuarioService)
    {
        // Map PaginationQuery to PaginationRequest
        var PaginationRequest = new PaginationRequest
        {
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            SortBy = pagination.SortBy,
            SortDirection = pagination.SortDirection ?? SortDirection.Asc,
            SearchTerm = pagination.SearchTerm,
            Filters = pagination.Filters
        };

        // ✅ Example: Chain multiple WithFilter calls for complex filtering
        PaginationRequest = PaginationRequest
            .WithFilter("IsDeleted", false)
            .WithFilter("CreatedAfter", DateTime.UtcNow.AddDays(-30))
            .WithFilter("MinimumRole", "User");

        return await usuarioService.GetPagedUsersComplexAsync(PaginationRequest).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetPagedUsersFromStoredProc(
        PaginationQuery pagination,
        IUsuarioService usuarioService,
        HttpContext httpContext)
    {
        // Map PaginationQuery to PaginationRequest
        var PaginationRequest = new PaginationRequest
        {
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            SortBy = pagination.SortBy,
            SortDirection = pagination.SortDirection ?? SortDirection.Asc,
            SearchTerm = pagination.SearchTerm,
            Filters = pagination.Filters
        };

        // Extract userId from claims and append to filters
        var userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            PaginationRequest = PaginationRequest.WithFilter("UserId", userId);
        }

        return await usuarioService.GetPagedUsersFromStoredProcAsync(PaginationRequest).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> BulkInsertUsers(
        List<UsuarioDto> users,
        IUsuarioService usuarioService)
    {
        return await usuarioService.BulkInsertUsersAsync(users).ToMinimalApiResultAsync("Usuarios insertados exitosamente.");
    }

    private static async Task<IResult> ExecuteBatchOperations(
        List<int> userIds,
        IUsuarioService usuarioService)
    {
        return await usuarioService.ExecuteBatchOperationsAsync(userIds).ToMinimalApiResultAsync("Operaciones batch ejecutadas exitosamente.");
    }

    #endregion

    #region Test Endpoints for Exception Handling

    private static async Task<IResult> GetUserWithException(int id, IUsuarioService usuarioService)
    {
        return await usuarioService.GetUserWithExceptionAsync(id).ToGetMinimalApiResultAsync();
    }

    private static async Task<IResult> GetUserWithCustomError(int id, IUsuarioService usuarioService)
    {
        return await usuarioService.GetUserWithCustomErrorAsync(id).ToGetMinimalApiResultAsync();
    }

    #endregion
}

