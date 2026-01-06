using System.Text.Json;

namespace Demo.Api.Endpoints.Core;

/// <summary>
/// Endpoints for MAC (Message Authentication Code) security operations.
/// Demonstrates HMAC-SHA256 and HMAC-SHA512 for API message integrity and authenticity.
/// </summary>
public static class MacSecurityEndpoints
{
    public static void MapMacSecurityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/mac")
            .WithTags("MAC Security");

        group.MapPost("/generate", GenerateMac)
            .WithName("GenerateMac")
            .WithSummary("Generate MAC signature for data")
            .WithDescription("Generates a MAC (HMAC-SHA256) signature for the provided data using a secret key.");

        group.MapPost("/generate-sha512", GenerateMacSha512)
            .WithName("GenerateMacSha512")
            .WithSummary("Generate MAC signature (SHA512) for data")
            .WithDescription("Generates a MAC (HMAC-SHA512) signature for the provided data using a secret key. Provides stronger security than SHA256.");

        group.MapPost("/verify", VerifyMac)
            .WithName("VerifyMac")
            .WithSummary("Verify MAC signature")
            .WithDescription("Verifies that a MAC signature is valid for the given data and secret key. Uses constant-time comparison to prevent timing attacks.");

        group.MapPost("/verify-sha512", VerifyMacSha512)
            .WithName("VerifyMacSha512")
            .WithSummary("Verify MAC signature (SHA512)")
            .WithDescription("Verifies that a MAC (SHA512) signature is valid for the given data and secret key.");

        group.MapPost("/generate-json", GenerateMacForJson)
            .WithName("GenerateMacForJson")
            .WithSummary("Generate MAC for JSON data")
            .WithDescription("Generates a MAC signature for JSON data. Automatically serializes the object before computing the MAC.");

        group.MapPost("/verify-json", VerifyMacForJson)
            .WithName("VerifyMacForJson")
            .WithSummary("Verify MAC for JSON data")
            .WithDescription("Verifies a MAC signature for JSON data.");
    }

    private static IResult GenerateMac(HttpContext httpContext, MacGenerateRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var mac = macService.GenerateMac(request.Data!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacResponse
            {
                Mac = mac,
                Algorithm = "HMAC-SHA256",
                Data = request.Data!,
                Timestamp = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_GENERATION_SHA512_ERROR", $"Failed to generate MAC (SHA512): {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }

    private static IResult GenerateMacSha512(HttpContext httpContext, MacGenerateRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var mac = macService.GenerateMacSha512(request.Data!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacResponse
            {
                Mac = mac,
                Algorithm = "HMAC-SHA512",
                Data = request.Data!,
                Timestamp = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_GENERATION_ERROR", $"Failed to generate MAC: {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }

    private static IResult VerifyMac(HttpContext httpContext, MacVerifyRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var isValid = macService.VerifyMac(request.Data!, request.Mac!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacVerificationResponse
            {
                IsValid = isValid,
                Algorithm = "HMAC-SHA256",
                Message = isValid ? "MAC signature is valid" : "MAC signature is invalid"
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_VERIFICATION_ERROR", $"Failed to verify MAC: {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }

    private static IResult VerifyMacSha512(HttpContext httpContext, MacVerifyRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var isValid = macService.VerifyMacSha512(request.Data!, request.Mac!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacVerificationResponse
            {
                IsValid = isValid,
                Algorithm = "HMAC-SHA512",
                Message = isValid ? "MAC signature is valid" : "MAC signature is invalid"
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_VERIFICATION_ERROR", $"Failed to verify MAC: {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }

    private static IResult GenerateMacForJson(HttpContext httpContext, MacJsonGenerateRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var mac = macService.GenerateMacForJson(request.JsonData!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacResponse
            {
                Mac = mac,
                Algorithm = "HMAC-SHA256",
                Data = JsonSerializer.Serialize(request.JsonData),
                Timestamp = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_JSON_GENERATION_ERROR", $"Failed to generate MAC for JSON: {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }

    private static IResult VerifyMacForJson(HttpContext httpContext, MacJsonVerifyRequest request)
    {
        var macService = httpContext.RequestServices.GetRequiredService<IMacSecurityService>();
        
        try
        {
            var isValid = macService.VerifyMacForJson(request.JsonData!, request.Mac!, request.SecretKey!);
            return Results.Ok(ApiResponse.Success(new MacVerificationResponse
            {
                IsValid = isValid,
                Algorithm = "HMAC-SHA256",
                Message = isValid ? "MAC signature is valid for JSON data" : "MAC signature is invalid for JSON data"
            }));
        }
        catch (Exception ex)
        {
            var error = new ApiError("MAC_JSON_VERIFICATION_ERROR", $"Failed to verify MAC for JSON: {ex.Message}");
            return Results.BadRequest(ApiResponse.Failure(error));
        }
    }
}

// Request/Response DTOs
public class MacGenerateRequest
{
    public string? Data { get; set; }
    public string? SecretKey { get; set; }
}

public class MacVerifyRequest
{
    public string? Data { get; set; }
    public string? Mac { get; set; }
    public string? SecretKey { get; set; }
}

public class MacJsonGenerateRequest
{
    public object? JsonData { get; set; }
    public string? SecretKey { get; set; }
}

public class MacJsonVerifyRequest
{
    public object? JsonData { get; set; }
    public string? Mac { get; set; }
    public string? SecretKey { get; set; }
}

public class MacResponse
{
    public string Mac { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class MacVerificationResponse
{
    public bool IsValid { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
