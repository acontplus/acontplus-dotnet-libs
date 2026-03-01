namespace Demo.Api.Endpoints.Infrastructure;

public static class ConfigurationTestEndpoints
{
    public static void MapConfigurationTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/configuration-test")
            .WithTags("Configuration Test");

        group.MapGet("/services", (ICacheService cache, IRequestContextService requestContext, IDeviceDetectionService deviceDetection, ISecurityHeaderService securityHeaders, HttpContext context) =>
        {
            try
            {
                var correlationId = requestContext.GetCorrelationId();
                var deviceType = deviceDetection.DetectDeviceType(context);
                var headers = securityHeaders.GetRecommendedHeaders(false);

                return Results.Ok(new
                {
                    Message = "All Acontplus.Services are working correctly!",
                    Services = new
                    {
                        CacheService = cache.GetType().Name,
                        RequestContextService = requestContext.GetType().Name,
                        DeviceDetectionService = deviceDetection.GetType().Name,
                        SecurityHeaderService = securityHeaders.GetType().Name
                    },
                    TestResults = new
                    {
                        CorrelationId = correlationId,
                        DeviceType = deviceType.ToString(),
                        SecurityHeadersCount = headers.Count,
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception)
            {
                return Results.Problem("Services test failed", statusCode: 500);
            }
        });

        group.MapGet("/cache", async (ICacheService cache) =>
        {
            try
            {
                var testKey = "test-config-key";
                var testValue = $"Test value generated at {DateTime.UtcNow:HH:mm:ss}";

                await cache.SetAsync(testKey, testValue, TimeSpan.FromMinutes(5));
                var retrievedValue = await cache.GetAsync<string>(testKey);

                return Results.Ok(new
                {
                    Message = "Cache service test completed successfully",
                    Test = new
                    {
                        Key = testKey,
                        SetValue = testValue,
                        RetrievedValue = retrievedValue,
                        CacheHit = retrievedValue == testValue
                    }
                });
            }
            catch (Exception)
            {
                return Results.Problem("Cache test failed", statusCode: 500);
            }
        });

        group.MapGet("/device", (IDeviceDetectionService deviceDetection, HttpContext context, HttpRequest request) =>
        {
            try
            {
                var userAgent = request.Headers.UserAgent.ToString();
                var deviceType = deviceDetection.DetectDeviceType(context);
                var isMobile = deviceDetection.IsMobileDevice(context);
                var capabilities = deviceDetection.GetDeviceCapabilities(userAgent);

                return Results.Ok(new
                {
                    Message = "Device detection test completed successfully",
                    Request = new
                    {
                        UserAgent = userAgent,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString()
                    },
                    Detection = new
                    {
                        DeviceType = deviceType.ToString(),
                        IsMobile = isMobile,
                        Capabilities = capabilities
                    }
                });
            }
            catch (Exception)
            {
                return Results.Problem("Device detection test failed", statusCode: 500);
            }
        });

        group.MapGet("/security", (ISecurityHeaderService securityHeaders, HttpContext context) =>
        {
            try
            {
                var isDevelopment = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

                var headers = securityHeaders.GetRecommendedHeaders(isDevelopment);
                var cspNonce = securityHeaders.GenerateCspNonce();

                return Results.Ok(new
                {
                    Message = "Security headers test completed successfully",
                    Environment = isDevelopment ? "Development" : "Production",
                    SecurityHeaders = headers,
                    CspNonce = cspNonce,
                    NonceLength = cspNonce.Length
                });
            }
            catch (Exception)
            {
                return Results.Problem("Security headers test failed", statusCode: 500);
            }
        });

        group.MapGet("/context", (IRequestContextService requestContext, HttpRequest request) =>
        {
            try
            {
                var context = requestContext.GetRequestContext();
                var correlationId = requestContext.GetCorrelationId();
                var clientId = requestContext.GetClientId();
                var tenantId = requestContext.GetTenantId();

                return Results.Ok(new
                {
                    Message = "Request context test completed successfully",
                    Context = new
                    {
                        CorrelationId = correlationId,
                        ClientId = clientId,
                        TenantId = tenantId,
                        RequestId = requestContext.GetRequestId(),
                        Timestamp = DateTime.UtcNow
                    },
                    Headers = new
                    {
                        XClientId = request.Headers["X-Client-ID"].ToString(),
                        XTenantId = request.Headers["X-Tenant-ID"].ToString()
                    }
                });
            }
            catch (Exception)
            {
                return Results.Problem("Request context test failed", statusCode: 500);
            }
        });
    }
}
