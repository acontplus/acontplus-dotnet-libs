namespace Acontplus.Services.Policies;

/// <summary>
/// Authorization requirement for device type validation and restrictions.
/// </summary>
/// <param name="allowedDeviceTypes">The list of allowed device types.</param>
/// <param name="requireDeviceValidation">Whether device header validation is required.</param>
public class DeviceTypeRequirement(List<DeviceType> allowedDeviceTypes, bool requireDeviceValidation = true) : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the list of allowed device types.
    /// </summary>
    public List<DeviceType> AllowedDeviceTypes { get; } = allowedDeviceTypes ?? throw new ArgumentNullException(nameof(allowedDeviceTypes));

    /// <summary>
    /// Gets a value indicating whether device headers must be validated.
    /// </summary>
    public bool RequireDeviceValidation { get; } = requireDeviceValidation;
}

/// <summary>
/// Authorization handler for device type validation.
/// </summary>
/// <param name="deviceDetectionService">The device detection service.</param>
/// <param name="logger">The logger instance.</param>
public class DeviceTypeHandler(
    IDeviceDetectionService deviceDetectionService,
    ILogger<DeviceTypeHandler> logger) : AuthorizationHandler<DeviceTypeRequirement>
{
    private readonly IDeviceDetectionService _deviceDetectionService = deviceDetectionService ?? throw new ArgumentNullException(nameof(deviceDetectionService));
    private readonly ILogger<DeviceTypeHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeviceTypeRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            _logger.LogWarning("Authorization context does not contain HttpContext");
            context.Fail();
            return Task.CompletedTask;
        }

        try
        {
            // Validate device headers if required
            if (requirement.RequireDeviceValidation && !_deviceDetectionService.ValidateDeviceHeaders(httpContext))
            {
                _logger.LogWarning("Device header validation failed");
                context.Fail();
                return Task.CompletedTask;
            }

            // Detect device type
            var deviceType = _deviceDetectionService.DetectDeviceType(httpContext);

            // Check if device type is allowed
            if (!requirement.AllowedDeviceTypes.Contains(deviceType))
            {
                _logger.LogWarning(
                    "Device type '{DeviceType}' is not allowed. Allowed types: {AllowedTypes}",
                    deviceType, string.Join(", ", requirement.AllowedDeviceTypes));
                context.Fail();
                return Task.CompletedTask;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Device type '{DeviceType}' validation successful", deviceType);
            }
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device type validation");
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Extension methods for registering device type authorization policies.
/// </summary>
public static class DeviceTypePolicyExtensions
{
    /// <summary>
    /// Registers the device type authorization handler into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeviceTypeAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, DeviceTypeHandler>();
        return services;
    }

    /// <summary>
    /// Adds preconfigured device type authorization policies to the authorization options.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <returns>The authorization options for chaining.</returns>
    public static AuthorizationOptions AddDeviceTypePolicies(this AuthorizationOptions options)
    {
        // Policy for mobile-only access
        options.AddPolicy("MobileOnly", policy =>
            policy.Requirements.Add(new DeviceTypeRequirement([DeviceType.Mobile])));

        // Policy for mobile and tablet access
        options.AddPolicy("MobileAndTablet", policy =>
            policy.Requirements.Add(new DeviceTypeRequirement(
                [DeviceType.Mobile, DeviceType.Tablet])));

        // Policy for desktop-only access
        options.AddPolicy("DesktopOnly", policy =>
            policy.Requirements.Add(new DeviceTypeRequirement([DeviceType.Desktop])));

        // Policy for all known device types (excludes Unknown)
        options.AddPolicy("KnownDevicesOnly", policy =>
            policy.Requirements.Add(new DeviceTypeRequirement(
                [DeviceType.Mobile, DeviceType.Tablet, DeviceType.Desktop, DeviceType.Web])));

        return options;
    }
}
