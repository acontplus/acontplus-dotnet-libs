namespace Acontplus.Services.Extensions.Security;

/// <summary>
/// Extension methods for opting into ASP.NET Core antiforgery protection in cookie-authenticated web flows.
/// </summary>
public static class AntiforgeryExtensions
{
    /// <summary>
    /// Registers ASP.NET Core antiforgery services using the framework defaults unless explicitly configured.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional configuration for the antiforgery cookie, header, and token behavior.</param>
    /// <returns>The configured service collection.</returns>
    /// <remarks>
    /// Register this only for APIs that use browser cookies for authentication. This registration does not protect
    /// every endpoint: apply <c>RequireAntiforgery()</c> only to the minimal API groups or endpoints that need it,
    /// or use the corresponding MVC antiforgery filters. Do not apply it to webhooks, machine-to-machine endpoints,
    /// or APIs authenticated solely with bearer tokens.
    /// </remarks>
    public static IServiceCollection AddAcontplusAntiforgery(
        this IServiceCollection services,
        Action<AntiforgeryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAntiforgery(options => configure?.Invoke(options));

        return services;
    }

    /// <summary>
    /// Adds antiforgery validation to the request pipeline and logs rejected protected requests.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The configured application builder.</returns>
    /// <remarks>
    /// Call after routing and before mapped minimal API endpoints. Invalid tokens receive ASP.NET Core's standard
    /// <c>400 Bad Request</c> response. The log entry deliberately excludes token values and request bodies.
    /// </remarks>
    public static IApplicationBuilder UseAcontplusAntiforgery(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AntiforgeryExtensions));

        app.Use(async (context, next) =>
        {
            await next();

            var validation = context.Features.Get<IAntiforgeryValidationFeature>();
            if (validation is { IsValid: false })
            {
                logger.LogWarning(
                    validation.Error,
                    "Antiforgery validation failed for {Method} {Path}. Response status: {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode);
            }
        });

        app.UseAntiforgery();

        return app;
    }

    /// <summary>
    /// Maps an optional endpoint that issues an antiforgery request token and stores its cookie token.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to configure.</param>
    /// <param name="pattern">The route pattern for the token endpoint.</param>
    /// <returns>The mapped route handler builder, allowing callers to require authorization or add metadata.</returns>
    /// <remarks>
    /// The caller is responsible for CORS and frontend-origin configuration. For authenticated browser applications,
    /// require authorization on the returned builder and fetch this endpoint with credentials included.
    /// </remarks>
    public static RouteHandlerBuilder MapAntiforgeryTokenEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/antiforgery/token")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet(pattern, (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            var requestToken = tokens.RequestToken
                ?? throw new InvalidOperationException("The antiforgery service did not issue a request token.");

            return TypedResults.Ok(new AntiforgeryTokenResponse(requestToken));
        })
        .Produces<AntiforgeryTokenResponse>(StatusCodes.Status200OK);
    }
}
