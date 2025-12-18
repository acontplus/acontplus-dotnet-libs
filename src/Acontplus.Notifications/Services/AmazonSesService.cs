using Acontplus.Core.Extensions;
using Acontplus.Notifications.Abstractions;
using Acontplus.Notifications.Models;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Template = Scriban.Template;

namespace Acontplus.Notifications.Services;

public sealed class AmazonSesService : IMailKitService, IDisposable
{
    private const int MaxSesBulkRecipients = 50;
    private const int DefaultMaxSendRate = 14; // SES default rate limit per region
    private const int DefaultBatchSize = 50;
    private const int DefaultBatchDelayMs = 100;

    // Email validation regex - more comprehensive
    private static readonly Regex EmailValidationRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private readonly IConfiguration _configuration;
    private readonly ILogger<AmazonSesService> _logger;
    private readonly IMemoryCache _templateCache;
    private readonly AmazonSimpleEmailServiceV2Client _sesClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncRetryPolicy _bulkRetryPolicy;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly Stopwatch _serviceStopwatch;

    // Rate limiting configuration
    private readonly int _maxSendRate;
    private readonly TimeSpan _rateLimitWindow;
    private readonly ConcurrentQueue<DateTime> _sendTimestamps;

    // Bulk sending configuration
    private readonly int _batchSize;
    private readonly TimeSpan _batchDelay;
    private readonly string? _defaultFromEmail;
    private readonly string? _mediaImagesPath;
    private readonly string? _templatesPath;

    public AmazonSesService(
        IConfiguration configuration,
        ILogger<AmazonSesService> logger,
        IMemoryCache? memoryCache = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache), "IMemoryCache must be registered in DI container");
        _serviceStopwatch = Stopwatch.StartNew();

        // Initialize SES v2 client with configuration
        var sesRegion = _configuration.GetValue("AWS:SES:Region", "us-east-1");
        _defaultFromEmail = _configuration.GetValue<string>("AWS:SES:DefaultFromEmail");
        _mediaImagesPath = _configuration.GetValue<string>("Media:ImagesPath");
        _templatesPath = _configuration.GetValue<string>("Templates:Path") ??
                         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

        var sesConfig = new AmazonSimpleEmailServiceV2Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(sesRegion),
            MaxErrorRetry = 5,
            Timeout = TimeSpan.FromSeconds(60),
            RetryMode = RequestRetryMode.Standard
        };

        // Use IAM roles or AWS SDK default credential chain
        var accessKey = _configuration.GetValue<string>("AWS:SES:AccessKey");
        var secretKey = _configuration.GetValue<string>("AWS:SES:SecretKey");

        _sesClient = !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)
            ? new AmazonSimpleEmailServiceV2Client(accessKey, secretKey, sesConfig)
            : new AmazonSimpleEmailServiceV2Client(sesConfig);

        // Configure rate limiting
        _maxSendRate = _configuration.GetValue("AWS:SES:MaxSendRate", DefaultMaxSendRate);
        _rateLimitWindow = TimeSpan.FromSeconds(1);
        _rateLimitSemaphore = new SemaphoreSlim(_maxSendRate, _maxSendRate);
        _sendTimestamps = new ConcurrentQueue<DateTime>();

        // Bulk sending configuration
        _batchSize = Math.Min(_configuration.GetValue("AWS:SES:BatchSize", DefaultBatchSize), MaxSesBulkRecipients);
        _batchDelay = TimeSpan.FromMilliseconds(_configuration.GetValue("AWS:SES:BatchDelayMs", DefaultBatchDelayMs));

        // Configure retry policies with circuit breaker pattern
        _retryPolicy = CreateRetryPolicy();
        _bulkRetryPolicy = CreateBulkRetryPolicy();
    }

    public async Task<bool> SendAsync(EmailModel email, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        using var activity = StartActivity("SendEmail");
        activity?.AddTag("recipient.count", 1);
        activity?.AddTag("email.subject", email.Subject);

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync(ct).ConfigureAwait(false);

                var sendRequest = await BuildSendEmailRequestAsync(email, ct).ConfigureAwait(false);

                _logger.LogDebug("Sending email via SES v2 to {RecipientEmail}", email.RecipientEmail);

                var response = await _sesClient.SendEmailAsync(sendRequest, ct).ConfigureAwait(false);

                RecordSendTimestamp();
                return true;
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to send email to {RecipientEmail}", email.RecipientEmail);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> SendBulkAsync(IEnumerable<EmailModel> emails, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(emails);

        var emailList = emails.ToList();
        if (emailList.Count == 0) return true;

        using var activity = StartActivity("SendBulkEmails");
        activity?.AddTag("recipient.count", emailList.Count);

        var batches = BatchEmails(emailList, _batchSize);
        var successCount = 0;
        var totalCount = emailList.Count;

        _logger.LogInformation("Starting bulk send of {TotalCount} emails in {BatchCount} batches",
            totalCount, batches.Count);

        foreach (var batch in batches)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Bulk email send cancelled");
                return false;
            }

            try
            {
                await EnforceRateLimitAsync(ct).ConfigureAwait(false);

                var batchResults = await ProcessBatchAsync(batch, ct).ConfigureAwait(false);
                successCount += batchResults.Count(s => s);

                RecordSendTimestamps(batchResults.Count(s => s));

                if (_batchDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_batchDelay, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing batch during bulk send");
                // Continue with next batch
            }
        }

        var success = successCount == totalCount;
        activity?.AddTag("emails.sent", successCount);
        activity?.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

        return success;
    }

    public async Task<bool> SendTemplatedBulkAsync(
        string templateName,
        IEnumerable<BulkEmailDestination> destinations,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(templateName);
        ArgumentNullException.ThrowIfNull(destinations);

        if (string.IsNullOrEmpty(_defaultFromEmail))
        {
            throw new InvalidOperationException("DefaultFromEmail must be configured for templated emails");
        }

        var destinationList = destinations.ToList();
        if (destinationList.Count == 0) return true;

        using var activity = StartActivity("SendTemplatedBulkEmails");
        activity?.AddTag("template.name", templateName);
        activity?.AddTag("recipient.count", destinationList.Count);

        // SES V2 has a maximum of 50 destinations per bulk email request
        var batches = BatchDestinations(destinationList, MaxSesBulkRecipients);
        var successCount = 0;

        _logger.LogInformation("Starting templated bulk send to {TotalCount} recipients in {BatchCount} batches",
            destinationList.Count, batches.Count);

        foreach (var batch in batches)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Templated bulk send cancelled");
                return false;
            }

            try
            {
                await EnforceRateLimitAsync(ct).ConfigureAwait(false);

                var request = new SendBulkEmailRequest
                {
                    FromEmailAddress = _defaultFromEmail,
                    DefaultContent = new BulkEmailContent
                    {
                        Template = new Amazon.SimpleEmailV2.Model.Template
                        {
                            TemplateName = templateName,
                            TemplateData = "{}" // Default empty JSON if no template data provided
                        }
                    },
                    BulkEmailEntries = batch.Select(dest => new BulkEmailEntry
                    {
                        Destination = new Destination
                        {
                            ToAddresses = dest.Destination.ToAddresses ?? [],
                            CcAddresses = dest.Destination.CcAddresses ?? [],
                            BccAddresses = dest.Destination.BccAddresses ?? []
                        },
                        ReplacementEmailContent = new ReplacementEmailContent
                        {
                            ReplacementTemplate = new ReplacementTemplate
                            {
                                ReplacementTemplateData = dest.ReplacementTemplateData ?? "{}"
                            }
                        }
                    }).ToList()
                };

                var response = await _bulkRetryPolicy.ExecuteAsync(
                    () => _sesClient.SendBulkEmailAsync(request, ct)).ConfigureAwait(false);

                var successfulMessages = response.BulkEmailEntryResults.Count(r =>
                    r.Status == BulkEmailStatus.SUCCESS);
                successCount += successfulMessages;

                RecordSendTimestamps(successfulMessages);

                if (_batchDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_batchDelay, ct).ConfigureAwait(false);
                }

                _logger.LogDebug("Batch completed with {SuccessCount}/{BatchSize} successes",
                    successfulMessages, batch.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to send templated batch for template '{TemplateName}'", templateName);
                // Continue with next batch
            }
        }

        var success = successCount == destinationList.Count;
        activity?.AddTag("emails.sent", successCount);
        activity?.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

        return success;
    }

    private async Task<SendEmailRequest> BuildSendEmailRequestAsync(EmailModel email, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(email.SenderEmail, nameof(email.SenderEmail));
        ArgumentException.ThrowIfNullOrEmpty(email.RecipientEmail, nameof(email.RecipientEmail));

        var request = new SendEmailRequest
        {
            FromEmailAddress = FormatEmailAddress(email.SenderName, email.SenderEmail),
            Destination = new Destination
            {
                ToAddresses = [],
                CcAddresses = [],
                BccAddresses = []
            }
        };

        // Parse and validate recipients
        var toAddresses = ParseAndValidateRecipients(email.RecipientEmail, "To");
        request.Destination.ToAddresses.AddRange(toAddresses);

        if (!string.IsNullOrEmpty(email.Cc))
        {
            var ccAddresses = ParseAndValidateRecipients(email.Cc, "Cc");
            request.Destination.CcAddresses.AddRange(ccAddresses);
        }

        request.Content = email.Files?.Count > 0
            ? await BuildRawEmailContentWithAttachmentsAsync(email, ct).ConfigureAwait(false)
            : await BuildSimpleEmailContentAsync(email, ct).ConfigureAwait(false);

        return request;
    }

    // Modificar el método BuildSimpleEmailContentAsync
    private async Task<EmailContent> BuildSimpleEmailContentAsync(EmailModel email, CancellationToken ct)
    {
        var emailContent = new EmailContent
        {
            Simple = new Message
            {
                Subject = new Content { Data = email.Subject, Charset = "UTF-8" },
                Body = new Body()
            }
        };

        string emailBody = email.Body;

        // Cambiar la condición: procesar template cuando IsHtml es true Y hay template
        if (!email.IsHtml && !string.IsNullOrEmpty(email.Template))
        {
            emailBody = await ProcessTemplateAsync(email.Template, email.Body, email.Logo, ct).ConfigureAwait(false);
        }

        emailContent.Simple.Body.Html = new Content
        {
            Data = emailBody,
            Charset = "UTF-8"
        };

        return emailContent;
    }

    // También modificar BuildRawEmailContentWithAttachmentsAsync
    private async Task<EmailContent> BuildRawEmailContentWithAttachmentsAsync(EmailModel email, CancellationToken ct)
    {
        var message = new StringBuilder();
        var boundary = $"----=_NextPart_{Guid.NewGuid():N}";
        var alternativeBoundary = $"----=_alt_{Guid.NewGuid():N}";

        // Headers (mantener igual)
        message.AppendLine($"From: {FormatEmailAddress(email.SenderName, email.SenderEmail ?? string.Empty)}");

        var toAddresses = ParseAndValidateRecipients(email.RecipientEmail, "To");
        message.AppendLine($"To: {string.Join(", ", toAddresses)}");

        if (!string.IsNullOrEmpty(email.Cc))
        {
            var ccAddresses = ParseAndValidateRecipients(email.Cc, "Cc");
            if (ccAddresses.Count > 0)
            {
                message.AppendLine($"Cc: {string.Join(", ", ccAddresses)}");
            }
        }

        message.AppendLine($"Subject: {email.Subject}");
        message.AppendLine("MIME-Version: 1.0");
        message.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        message.AppendLine();

        // Start multipart/alternative for text/plain and text/html
        message.AppendLine($"--{boundary}");
        message.AppendLine($"Content-Type: multipart/alternative; boundary=\"{alternativeBoundary}\"");
        message.AppendLine();

        // Plain text part
        message.AppendLine($"--{alternativeBoundary}");
        message.AppendLine("Content-Type: text/plain; charset=UTF-8");
        message.AppendLine("Content-Transfer-Encoding: quoted-printable");
        message.AppendLine();
        message.AppendLine(WebUtility.HtmlDecode(email.Body));
        message.AppendLine();

        message.AppendLine($"--{alternativeBoundary}");
        message.AppendLine("Content-Type: text/html; charset=UTF-8");
        message.AppendLine("Content-Transfer-Encoding: base64");
        message.AppendLine();

        // Cambiar la condición aquí también
        var bodyContent = !email.IsHtml && !string.IsNullOrEmpty(email.Template)
            ? await ProcessTemplateAsync(email.Template, email.Body, email.Logo, ct).ConfigureAwait(false)
            : email.Body;

        var htmlBytes = Encoding.UTF8.GetBytes(bodyContent);
        var htmlBase64 = Convert.ToBase64String(htmlBytes);
        AppendBase64Content(message, htmlBase64);
        message.AppendLine();

        message.AppendLine($"--{alternativeBoundary}--");

        // Attachments (resto igual)
        if (email.Files?.Count > 0)
        {
            foreach (var file in email.Files)
            {
                if (file.Content is null or { Length: 0 })
                {
                    _logger.LogWarning("Skipping empty attachment '{FileName}'", file.FileName);
                    continue;
                }

                message.AppendLine($"--{boundary}");
                message.AppendLine($"Content-Type: {GetMimeType(file.FileName ?? string.Empty)}; name=\"{file.FileName}\"");
                message.AppendLine("Content-Transfer-Encoding: base64");
                message.AppendLine($"Content-Disposition: attachment; filename=\"{file.FileName}\"");
                message.AppendLine();

                var base64Content = Convert.ToBase64String(file.Content);
                AppendBase64Content(message, base64Content);
                message.AppendLine();
            }
        }

        message.AppendLine($"--{boundary}--");

        return new EmailContent
        {
            Raw = new RawMessage
            {
                Data = new MemoryStream(Encoding.UTF8.GetBytes(message.ToString()))
            }
        };
    }
    private async Task<string> ProcessTemplateAsync(
        string templateName,
        string jsonData,
        string? logo,
        CancellationToken ct)
    {
        var templatePath = Path.Combine(_templatesPath ?? string.Empty, templateName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }

        // Try to get template content from cache
        var cacheKey = $"email_template:{templateName}";
        var templateContent = await _templateCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            entry.Priority = CacheItemPriority.Normal;

            _logger.LogDebug("Loading and caching template: {TemplateName}", templateName);
            return await File.ReadAllTextAsync(templatePath, ct).ConfigureAwait(false);
        });

        var templateData = string.IsNullOrEmpty(jsonData)
            ? new Dictionary<string, object>()
            : JsonExtensions.DeserializeOptimized<IDictionary<string, object>>(jsonData) ?? new Dictionary<string, object>();

        // Process logo if provided
        if (!string.IsNullOrEmpty(logo))
        {
            ProcessLogoInTemplate(templateData, logo, ct);
        }

        return ProcessTemplate(templateContent ?? throw new InvalidOperationException("Template content is null"), templateData);
    }

    private void ProcessLogoInTemplate(IDictionary<string, object> templateData, string logo, CancellationToken ct)
    {
        //if (string.IsNullOrEmpty(_mediaImagesPath)) return;

        //var logoPath = Path.Combine(_mediaImagesPath, "Logos", logo);
        //if (!File.Exists(logoPath)) return;

        try
        {
            //var logoBytes = await File.ReadAllBytesAsync(logoPath, ct).ConfigureAwait(false);
            //var logoBase64 = Convert.ToBase64String(logoBytes);
            //var logoMimeType = GetMimeType(logoPath);
            //var logoDataUri = $"data:{logoMimeType};base64,{logoBase64}";
            templateData["imgLogo"] = logo;

            //_logger.LogDebug("Logo processed successfully: {LogoPath} -> {MimeType}", logoPath, logoMimeType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process logo '{Logo}'", logo);
            templateData["imgLogo"] = "";
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            CleanupOldTimestamps();

            if (_sendTimestamps.Count >= _maxSendRate)
            {
                if (_sendTimestamps.TryPeek(out var oldestTimestamp))
                {
                    var timeToWait = oldestTimestamp + _rateLimitWindow - DateTime.UtcNow;
                    if (timeToWait > TimeSpan.Zero)
                    {
                        _logger.LogDebug("Rate limit reached ({CurrentCount}/{MaxRate}), waiting {WaitTime}",
                            _sendTimestamps.Count, _maxSendRate, timeToWait);

                        await Task.Delay(timeToWait, ct).ConfigureAwait(false);
                        CleanupOldTimestamps();
                    }
                }
            }
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private void CleanupOldTimestamps()
    {
        var cutoff = DateTime.UtcNow - _rateLimitWindow;
        while (_sendTimestamps.TryPeek(out var timestamp) && timestamp < cutoff)
        {
            _sendTimestamps.TryDequeue(out _);
        }
    }

    private void RecordSendTimestamp() => _sendTimestamps.Enqueue(DateTime.UtcNow);

    private void RecordSendTimestamps(int count)
    {
        var now = DateTime.UtcNow;
        for (int i = 0; i < count; i++)
        {
            _sendTimestamps.Enqueue(now);
        }
    }

    private AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<TooManyRequestsException>()
            .Or<BadRequestException>(ex => IsTransientBadRequest(ex))
            .Or<InternalServiceErrorException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                sleepDurations: [
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(8),
                    TimeSpan.FromSeconds(16)
                ],
                onRetryAsync: (exception, delay, retryCount, _) =>
                {
                    _logger.LogWarning(exception,
                        "Retry attempt {RetryCount} after {Delay} for SES operation",
                        retryCount, delay);
                    return Task.CompletedTask;
                });
    }

    private AsyncRetryPolicy CreateBulkRetryPolicy()
    {
        return Policy
            .Handle<TooManyRequestsException>()
            .Or<InternalServiceErrorException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                sleepDurations: [
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(8)
                ],
                onRetryAsync: (exception, delay, retryCount, _) =>
                {
                    _logger.LogWarning(exception,
                        "Retry attempt {RetryCount} after {Delay} for SES bulk operation",
                        retryCount, delay);
                    return Task.CompletedTask;
                });
    }

    private static bool IsTransientBadRequest(BadRequestException ex)
    {
        return ex.Message.Contains("throttl", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("concurrent", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatEmailAddress(string? name, string email)
    {
        return string.IsNullOrEmpty(name) ? email : $"{name} <{email}>";
    }

    private static List<string> ParseAndValidateRecipients(string recipients, string type = "Recipient")
    {
        if (string.IsNullOrWhiteSpace(recipients))
        {
            throw new ArgumentException($"{type} recipients cannot be null or empty", nameof(recipients));
        }

        var parsed = recipients.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              .Where(r => !string.IsNullOrWhiteSpace(r))
                              .ToList();

        if (parsed.Count == 0)
        {
            throw new ArgumentException($"No valid {type.ToLower()} recipients found", nameof(recipients));
        }

        // Validate email addresses
        var validEmails = new List<string>();
        foreach (var email in parsed)
        {
            if (IsValidEmail(email))
            {
                validEmails.Add(email);
            }
            else
            {
                throw new ArgumentException($"Invalid email address format: {email}");
            }
        }

        return validEmails;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return EmailValidationRegex.IsMatch(email);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string ProcessTemplate(string template, IDictionary<string, object> data)
    {
        // Primero hacer reemplazos directos para sintaxis [key]
        string processedTemplate = template;
        foreach (var kvp in data)
        {
            var placeholder = $"[{kvp.Key}]";
            if (processedTemplate.Contains(placeholder))
            {
                processedTemplate = processedTemplate.Replace(placeholder, kvp.Value?.ToString() ?? "");
            }
        }

        // Luego procesar con Scriban para sintaxis {{ key }}
        var scriptObject = new ScriptObject();
        foreach (var prop in data)
        {
            scriptObject.Add(LowerFirstCharacter(prop.Key), prop.Value);
        }

        var templateObj = Template.Parse(processedTemplate);
        return templateObj.Render(scriptObject);
    }

    private static string LowerFirstCharacter(string value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : value.Length > 1
            ? char.ToLowerInvariant(value[0]) + value[1..]
            : char.ToLowerInvariant(value[0]).ToString();
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName.AsSpan());

        return extension switch
        {
            var ext when ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Image.Jpeg,
            var ext when ext.Equals(".png", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Image.Png,
            var ext when ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Image.Gif,
            var ext when ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Application.Pdf,
            var ext when ext.Equals(".txt", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Text.Plain,
            var ext when ext.Equals(".html", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Text.Html,
            var ext when ext.Equals(".xml", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Text.Xml,
            var ext when ext.Equals(".csv", StringComparison.OrdinalIgnoreCase) => "text/csv",
            var ext when ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) => MediaTypeNames.Application.Zip,
            var ext when ext.Equals(".json", StringComparison.OrdinalIgnoreCase) => "application/json",
            _ => MediaTypeNames.Application.Octet
        };
    }

    private static void AppendBase64Content(StringBuilder sb, string base64Content)
    {
        const int lineLength = 76;
        for (int i = 0; i < base64Content.Length; i += lineLength)
        {
            var length = Math.Min(lineLength, base64Content.Length - i);
            sb.AppendLine(base64Content.Substring(i, length));
        }
    }

    private static List<List<T>> BatchEmails<T>(List<T> source, int size) where T : class
    {
        var batches = new List<List<T>>();
        for (int i = 0; i < source.Count; i += size)
        {
            batches.Add(source.GetRange(i, Math.Min(size, source.Count - i)));
        }
        return batches;
    }

    private static List<List<BulkEmailDestination>> BatchDestinations(List<BulkEmailDestination> source, int size)
    {
        var batches = new List<List<BulkEmailDestination>>();
        for (int i = 0; i < source.Count; i += size)
        {
            batches.Add(source.GetRange(i, Math.Min(size, source.Count - i)));
        }
        return batches;
    }

    private async Task<List<bool>> ProcessBatchAsync(List<EmailModel> batch, CancellationToken ct)
    {
        var tasks = batch.Select(email => SendEmailWithRetryAsync(email, ct)).ToList();
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<bool> SendEmailWithRetryAsync(EmailModel email, CancellationToken ct)
    {
        try
        {
            var sendRequest = await BuildSendEmailRequestAsync(email, ct).ConfigureAwait(false);
            await _sesClient.SendEmailAsync(sendRequest, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", email.RecipientEmail);
            return false;
        }
    }

    private Activity? StartActivity([CallerMemberName] string operationName = "")
    {
        var activity = new Activity(operationName);
        activity.SetTag("service", "AmazonSES");
        activity.SetTag("service.version", "v2");
        activity.SetTag("service.uptime", _serviceStopwatch.Elapsed);
        return activity.Start();
    }

    public void Dispose()
    {
        _sesClient?.Dispose();
        _rateLimitSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    public sealed class BulkEmailDestination
    {
        public Destination Destination { get; set; } = new();
        public string? ReplacementTemplateData { get; set; }
    }
}
