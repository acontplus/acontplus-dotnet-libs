namespace Acontplus.Services.Policies;

/// <summary>
/// Authorization requirement for tenant isolation and validation.
/// </summary>
/// <param name="requireTenantHeader">Whether the Tenant-Id header is required.</param>
/// <param name="validateUserTenantAccess">Whether the user's tenant claim should be validated.</param>
public class TenantIsolationRequirement(bool requireTenantHeader = true, bool validateUserTenantAccess = true) : IAuthorizationRequirement
{
    /// <summary>
    /// Gets a value indicating whether the Tenant-Id header is strictly required.
    /// </summary>
    public bool RequireTenantHeader { get; } = requireTenantHeader;

    /// <summary>
    /// Gets a value indicating whether the user's tenant claim must match the header tenant ID.
    /// </summary>
    public bool ValidateUserTenantAccess { get; } = validateUserTenantAccess;
}

/// <summary>
/// Authorization handler for tenant isolation validation.
/// </summary>
/// <param name="logger">The logger instance.</param>
public class TenantIsolationHandler(ILogger<TenantIsolationHandler> logger) : AuthorizationHandler<TenantIsolationRequirement>
{
    private readonly ILogger<TenantIsolationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantIsolationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            _logger.LogWarning("Authorization context does not contain HttpContext");
            context.Fail();
            return Task.CompletedTask;
        }

        // Get Tenant-Id from headers
        if (!httpContext.Request.Headers.TryGetValue("Tenant-Id", out var tenantIdHeader))
        {
            if (requirement.RequireTenantHeader)
            {
                _logger.LogWarning("Tenant-Id header is required but not provided");
                context.Fail();
                return Task.CompletedTask;
            }

            _logger.LogDebug("No Tenant-Id header found, but not required");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var tenantId = tenantIdHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Tenant-Id header is empty");
            context.Fail();
            return Task.CompletedTask;
        }

        // Validate user has access to the specified tenant
        if (requirement.ValidateUserTenantAccess && context.User.Identity?.IsAuthenticated == true)
        {
            var userTenantId = context.User.GetClaimValue<string>("tenant_id");
            if (!string.IsNullOrEmpty(userTenantId) &&
                !string.Equals(userTenantId, tenantId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "User tenant '{UserTenant}' does not match requested tenant '{RequestedTenant}'",
                    userTenantId, tenantId);
                context.Fail();
                return Task.CompletedTask;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Tenant isolation validation successful for tenant '{TenantId}'", tenantId);
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for registering tenant isolation authorization policies.
/// </summary>
public static class TenantIsolationPolicyExtensions
{
    /// <summary>
    /// Registers the tenant isolation authorization handler into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantIsolationAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, TenantIsolationHandler>();
        return services;
    }

    /// <summary>
    /// Adds preconfigured tenant isolation authorization policies to the authorization options.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <returns>The authorization options for chaining.</returns>
    public static AuthorizationOptions AddTenantIsolationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy("RequireTenant", policy =>
            policy.Requirements.Add(new TenantIsolationRequirement()));

        options.AddPolicy("ValidateTenantAccess", policy =>
            policy.Requirements.Add(new TenantIsolationRequirement(validateUserTenantAccess: true)));

        options.AddPolicy("OptionalTenant", policy =>
            policy.Requirements.Add(new TenantIsolationRequirement(requireTenantHeader: false)));

        return options;
    }
}
