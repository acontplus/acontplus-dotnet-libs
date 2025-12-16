using Acontplus.Notifications.Abstractions;
using Acontplus.Notifications.Models;
using Acontplus.S3Application.Interfaces;
using Acontplus.S3Application.Models;

namespace Demo.Api.Endpoints.Demo;

/// <summary>
/// Demonstrates S3 storage and email notification features with scalability improvements.
/// </summary>
public static class StorageAndNotificationsEndpoints
{
    public static RouteGroupBuilder MapStorageAndNotificationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/upload-and-notify", UploadFileAndNotify)
            .WithName("UploadAndNotify")
            .WithDescription("Uploads a file to S3 and sends email notification")
            .WithOpenApi();

        group.MapGet("/presigned-url/{fileName}", GeneratePresignedUrl)
            .WithName("GeneratePresignedUrl")
            .WithDescription("Generates a presigned URL for file download")
            .WithOpenApi();

        group.MapPost("/send-templated-email", SendTemplatedEmail)
            .WithName("SendTemplatedEmail")
            .WithDescription("Sends email using cached template (v1.5.0 feature)")
            .WithOpenApi();

        group.MapPost("/bulk-upload", BulkUpload)
            .WithName("BulkUpload")
            .WithDescription("Demonstrates bulk file upload with connection pooling")
            .WithOpenApi();

        return group;
    }

    /// <summary>
    /// Uploads a file to S3 and sends email notification.
    /// Demonstrates S3 v2.0.0 features: connection pooling, retry policy, rate limiting.
    /// </summary>
    private static async Task<IResult> UploadFileAndNotify(
        IFormFile file,
        IS3StorageService s3Service,
        IMailKitService emailService,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            // Upload to S3 with automatic retry and rate limiting
            var s3Object = new S3ObjectCustom(configuration);
            await s3Object.Initialize("demo-uploads/", file);

            logger.LogInformation("Uploading file {FileName} ({Size} bytes) to S3",
                file.FileName, file.Length);

            var uploadResponse = await s3Service.UploadAsync(s3Object);

            if (uploadResponse.StatusCode != 201)
            {
                logger.LogError("S3 upload failed: {Message}", uploadResponse.Message);
                return Results.StatusCode(uploadResponse.StatusCode);
            }

            // Generate presigned URL for sharing
            var urlResponse = await s3Service.GetPresignedUrlAsync(s3Object, expirationInMinutes: 60);
            var downloadUrl = urlResponse.FileName;

            logger.LogInformation("File uploaded successfully. Presigned URL: {Url}", downloadUrl);

            // Send email notification with template caching (v1.5.0)
            var email = new EmailModel
            {
                SmtpServer = "not-used-for-ses", // Required by model but not used with SES
                Password = "not-used-for-ses", // Required by model but not used with SES
                SenderEmail = configuration["AWS:SES:DefaultFromEmail"] ?? "noreply@example.com",
                RecipientEmail = "admin@example.com",
                Subject = $"File Uploaded: {file.FileName}",
                Template = "file-upload-notification.html", // Cached for 30 minutes!
                Body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    FileName = file.FileName,
                    FileSize = $"{file.Length / 1024.0:F2} KB",
                    UploadDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    DownloadUrl = downloadUrl,
                    ExpiresIn = "60 minutes"
                }),
                IsHtml = false // Will process template
            };

            var emailSent = await emailService.SendAsync(email, ct);

            return Results.Ok(new
            {
                success = true,
                message = $"File '{file.FileName}' uploaded successfully",
                s3Response = new
                {
                    uploadResponse.StatusCode,
                    uploadResponse.Message,
                    url = s3Object.S3ObjectUrl,
                    downloadUrl
                },
                emailSent,
                features = new
                {
                    s3ConnectionPooling = "Enabled - Reuses clients per region/credentials",
                    s3RetryPolicy = "Enabled - 3 retries with exponential backoff",
                    s3RateLimit = "Enabled - 100 req/s default",
                    templateCaching = "Enabled - 30 min cache, 50x faster"
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in upload and notify operation");
            return Results.Problem(ex.Message);
        }
    }

    /// <summary>
    /// Generates a presigned URL for temporary file access.
    /// </summary>
    private static async Task<IResult> GeneratePresignedUrl(
        string fileName,
        IS3StorageService s3Service,
        IConfiguration configuration,
        int expirationMinutes = 30)
    {
        try
        {
            var s3Object = new S3ObjectCustom(configuration);
            s3Object.Initialize($"demo-uploads/{fileName}");

            // Check if file exists first
            var exists = await s3Service.DoesObjectExistAsync(s3Object);
            if (!exists)
            {
                return Results.NotFound(new { message = $"File '{fileName}' not found in S3" });
            }

            var response = await s3Service.GetPresignedUrlAsync(s3Object, expirationMinutes);

            if (response.StatusCode == 200)
            {
                return Results.Ok(new
                {
                    fileName,
                    presignedUrl = response.FileName,
                    expiresIn = $"{expirationMinutes} minutes",
                    expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                });
            }

            return Results.StatusCode(response.StatusCode);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    /// <summary>
    /// Sends email using template (demonstrates v1.5.0 template caching).
    /// </summary>
    private static async Task<IResult> SendTemplatedEmail(
        [FromBody] TemplatedEmailRequest request,
        IMailKitService emailService,
        IConfiguration configuration,
        CancellationToken ct)
    {
        try
        {
            var email = new EmailModel
            {
                SmtpServer = "not-used-for-ses", // Required by model but not used with SES
                Password = "not-used-for-ses", // Required by model but not used with SES
                SenderEmail = configuration["AWS:SES:DefaultFromEmail"] ?? "noreply@example.com",
                RecipientEmail = request.To,
                Subject = request.Subject,
                Template = request.TemplateName,
                Body = System.Text.Json.JsonSerializer.Serialize(request.TemplateData),
                IsHtml = false
            };

            var success = await emailService.SendAsync(email, ct);

            return Results.Ok(new
            {
                success,
                message = success ? "Email sent successfully" : "Failed to send email",
                templateCached = "Template loaded from memory cache if previously used (30min TTL)",
                performance = "First send: ~10-50ms template load. Subsequent: <1ms (50x faster)"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    /// <summary>
    /// Bulk file upload demonstrating connection pooling and rate limiting.
    /// </summary>
    private static async Task<IResult> BulkUpload(
        List<IFormFile> files,
        IS3StorageService s3Service,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        if (files == null || !files.Any())
        {
            return Results.BadRequest("No files provided");
        }

        logger.LogInformation("Starting bulk upload of {Count} files", files.Count);
        var startTime = DateTime.UtcNow;

        var results = new List<object>();

        // S3 service automatically:
        // 1. Pools connections (reuses clients)
        // 2. Rate limits to 100 req/s
        // 3. Retries on transient failures
        foreach (var file in files)
        {
            try
            {
                var s3Object = new S3ObjectCustom(configuration);
                await s3Object.Initialize("demo-bulk/", file);

                var response = await s3Service.UploadAsync(s3Object);

                results.Add(new
                {
                    fileName = file.FileName,
                    success = response.StatusCode == 201,
                    statusCode = response.StatusCode,
                    message = response.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                results.Add(new
                {
                    fileName = file.FileName,
                    success = false,
                    error = ex.Message
                });
            }
        }

        var duration = DateTime.UtcNow - startTime;
        var successCount = results.Count(r => (bool)r.GetType().GetProperty("success")!.GetValue(r)!);

        return Results.Ok(new
        {
            totalFiles = files.Count,
            successCount,
            failedCount = files.Count - successCount,
            durationSeconds = duration.TotalSeconds,
            throughput = $"{files.Count / duration.TotalSeconds:F2} files/sec",
            results,
            features = new
            {
                connectionPooling = "S3 clients reused across uploads",
                rateLimiting = "Automatically throttled to prevent AWS errors",
                retryPolicy = "Transient failures retried with exponential backoff"
            }
        });
    }

    public record TemplatedEmailRequest(
        string To,
        string Subject,
        string TemplateName,
        Dictionary<string, object> TemplateData
    );
}
