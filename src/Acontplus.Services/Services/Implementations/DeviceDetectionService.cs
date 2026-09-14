using System.Text.RegularExpressions;

namespace Acontplus.Services.Services.Implementations;

/// <summary>
/// Implementation of device detection service for identifying device types and capabilities.
/// </summary>
public class DeviceDetectionService : IDeviceDetectionService
{
    private readonly ILogger<DeviceDetectionService> _logger;

    // Regex patterns for device detection
    private static readonly Regex MobilePattern = new(
        @"(Mobile|Android|iPhone|iPad|iPod|BlackBerry|Windows Phone|Opera Mini)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TabletPattern = new(
        @"(iPad|Android(?!.*Mobile)|Tablet)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DesktopPattern = new(
        @"(Windows NT|Macintosh|Linux(?!.*Android))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceDetectionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DeviceDetectionService(ILogger<DeviceDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DeviceType DetectDeviceType(HttpContext context)
    {
        // Try Device-Type header first (preferred method)
        if (context.Request.Headers.TryGetValue("Device-Type", out var deviceTypeHeader) &&
            Enum.TryParse<DeviceType>(deviceTypeHeader.FirstOrDefault(), ignoreCase: true, out var parsedType))
        {
            _logger.LogDebug("Device type detected from header: {DeviceType}", parsedType);
            return parsedType;
        }

        // Fall back to legacy X-Is-Mobile header
        if (context.Request.Headers.TryGetValue("X-Is-Mobile", out var isMobileHeader) &&
            bool.TryParse(isMobileHeader.FirstOrDefault(), out var isMobile))
        {
            var legacyType = isMobile ? DeviceType.Mobile : DeviceType.Desktop;
            _logger.LogDebug("Device type detected from legacy header: {DeviceType}", legacyType);
            return legacyType;
        }

        // Final fallback to user agent detection
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var detectedType = DetectFromUserAgent(userAgent);

        _logger.LogDebug("Device type detected from user agent: {DeviceType}", detectedType);
        return detectedType;
    }

    /// <inheritdoc />
    public bool IsMobileDevice(HttpContext context)
    {
        var deviceType = DetectDeviceType(context);
        return deviceType is DeviceType.Mobile or DeviceType.Tablet;
    }

    /// <inheritdoc />
    public DeviceCapabilities GetDeviceCapabilities(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new DeviceCapabilities(
                DeviceType.Unknown, false, false, false, null, null, null);
        }

        var deviceType = DetectFromUserAgent(userAgent);
        var isMobile = deviceType == DeviceType.Mobile;
        var isTablet = deviceType == DeviceType.Tablet;
        var supportsTouch = isMobile || isTablet || userAgent.Contains("Touch", StringComparison.OrdinalIgnoreCase);

        var os = DetectOperatingSystem(userAgent);
        var browser = DetectBrowser(userAgent);
        var version = DetectBrowserVersion(userAgent, browser);

        var capabilities = new DeviceCapabilities(
            deviceType, isMobile, isTablet, supportsTouch, os, browser, version);

        _logger.LogDebug("Detected device capabilities: {Capabilities}", capabilities);
        return capabilities;
    }

    /// <inheritdoc />
    public bool ValidateDeviceHeaders(HttpContext context)
    {
        var headers = context.Request.Headers;

        // Validate Device-Type header if present
        if (headers.TryGetValue("Device-Type", out var deviceTypeHeader))
        {
            var deviceTypeValue = deviceTypeHeader.FirstOrDefault();
            if (!Enum.TryParse<DeviceType>(deviceTypeValue, ignoreCase: true, out _))
            {
                _logger.LogWarning("Invalid Device-Type header value: {Value}", deviceTypeValue);
                return false;
            }
        }

        // Validate X-Is-Mobile header if present
        if (headers.TryGetValue("X-Is-Mobile", out var isMobileHeader))
        {
            var isMobileValue = isMobileHeader.FirstOrDefault();
            if (!bool.TryParse(isMobileValue, out _))
            {
                _logger.LogWarning("Invalid X-Is-Mobile header value: {Value}", isMobileValue);
                return false;
            }
        }

        _logger.LogDebug("Device headers validation passed");
        return true;
    }

    private DeviceType DetectFromUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return DeviceType.Unknown;

        // Check for tablet first (more specific)
        if (TabletPattern.IsMatch(userAgent))
            return DeviceType.Tablet;

        // Check for mobile
        if (MobilePattern.IsMatch(userAgent))
            return DeviceType.Mobile;

        // Check for desktop
        if (DesktopPattern.IsMatch(userAgent))
            return DeviceType.Desktop;

        // Default to web for unknown patterns
        return DeviceType.Web;
    }

    private static string? DetectOperatingSystem(string userAgent)
    {
        return userAgent switch
        {
            var ua when ua.Contains("Windows NT", StringComparison.OrdinalIgnoreCase) => "Windows",
            var ua when ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) => "macOS",
            var ua when ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) => "Linux",
            var ua when ua.Contains("Android", StringComparison.OrdinalIgnoreCase) => "Android",
            var ua when ua.Contains("iPhone OS", StringComparison.OrdinalIgnoreCase) => "iOS",
            var ua when ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) => "iPadOS",
            _ => null
        };
    }

    private static string? DetectBrowser(string userAgent)
    {
        return userAgent switch
        {
            var ua when ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase) &&
                       !ua.Contains("Edge", StringComparison.OrdinalIgnoreCase) => "Chrome",
            var ua when ua.Contains("Firefox", StringComparison.OrdinalIgnoreCase) => "Firefox",
            var ua when ua.Contains("Safari", StringComparison.OrdinalIgnoreCase) &&
                       !ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase) => "Safari",
            var ua when ua.Contains("Edge", StringComparison.OrdinalIgnoreCase) => "Edge",
            var ua when ua.Contains("Opera", StringComparison.OrdinalIgnoreCase) => "Opera",
            _ => null
        };
    }

    private static string? DetectBrowserVersion(string userAgent, string? browser)
    {
        if (string.IsNullOrEmpty(browser))
            return null;

        try
        {
            var pattern = browser switch
            {
                "Chrome" => @"Chrome/(\d+\.\d+)",
                "Firefox" => @"Firefox/(\d+\.\d+)",
                "Safari" => @"Version/(\d+\.\d+)",
                "Edge" => @"Edge/(\d+\.\d+)",
                "Opera" => @"Opera/(\d+\.\d+)",
                _ => null
            };

            if (pattern != null)
            {
                var match = Regex.Match(userAgent, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }
        }
        catch (Exception)
        {
            // Ignore regex errors
        }

        return null;
    }
}
