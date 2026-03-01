namespace Demo.Api.Endpoints.Core;

public static class EncryptionEndpoints
{
    public static void MapEncryptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/encryption")
            .WithTags("Encryption");

        group.MapPost("/encrypt", async (HttpContext httpContext, EncryptRequest request) =>
        {
            var sensitiveDataEncryptionService = httpContext.RequestServices.GetRequiredService<ISensitiveDataEncryptionService>();
            var encryptedBytes = await sensitiveDataEncryptionService.EncryptToBytesAsync("ivan", request.PlainText!);
            return Results.Ok(ApiResponse.Success(Convert.ToBase64String(encryptedBytes)));
        });

        group.MapPost("/decrypt", async (HttpContext httpContext, DecryptRequest request) =>
        {
            var sensitiveDataEncryptionService = httpContext.RequestServices.GetRequiredService<ISensitiveDataEncryptionService>();
            var encryptedBytes = Convert.FromBase64String(request.EncryptedData!);
            var decryptedData = await sensitiveDataEncryptionService.DecryptFromBytesAsync("ivan", encryptedBytes);
            return Results.Ok(ApiResponse.Success(decryptedData));
        });

        group.MapPost("/hash", (HttpContext httpContext, HashRequest request) =>
        {
            var passwordHashingService = httpContext.RequestServices.GetRequiredService<IPasswordSecurityService>();
            var hashedPassword = passwordHashingService.HashPassword(request.Password!);
            return Results.Ok(ApiResponse.Success(hashedPassword));
        });

        group.MapPost("/setpassword", (HttpContext httpContext, SetPasswordRequest request) =>
        {
            var dataSecurityService = httpContext.RequestServices.GetRequiredService<IPasswordSecurityService>();
            var result = dataSecurityService.SetPassword(request.Password!);
            return Results.Ok(ApiResponse.Success(new { EncryptedPassword = Convert.ToBase64String(result.EncryptedPassword), result.PasswordHash }));
        });

        group.MapPost("/decryptpassword", (HttpContext httpContext, EncryptedPasswordRequest request) =>
        {
            var dataSecurityService = httpContext.RequestServices.GetRequiredService<IPasswordSecurityService>();
            var encryptedPasswordBytes = Convert.FromBase64String(request.EncryptedPassword!);
            var decryptedPassword = dataSecurityService.GetDecryptedPassword(encryptedPasswordBytes);
            return Results.Ok(ApiResponse.Success(decryptedPassword));
        });

        group.MapPost("/verifypassword", (HttpContext httpContext, VerifyPasswordRequest request) =>
        {
            var dataSecurityService = httpContext.RequestServices.GetRequiredService<IPasswordSecurityService>();
            var isValid = dataSecurityService.VerifyPassword(request.Password!, request.PasswordHash!);
            return Results.Ok(ApiResponse.Success(isValid));
        });
    }
}

public class EncryptRequest
{
    public string? PlainText { get; set; }
}

public class DecryptRequest
{
    public string? EncryptedData { get; set; }
}

public class HashRequest
{
    public string? Password { get; set; }
}

public class SetPasswordRequest
{
    public string? Password { get; set; }
}

public class EncryptedPasswordRequest
{
    public string? EncryptedPassword { get; set; }
}

public class VerifyPasswordRequest
{
    public string? Password { get; set; }
    public string? PasswordHash { get; set; }
}
