using System.Collections.Concurrent;
using System.Dynamic;
using System.Security.Authentication;
using System.Text.Json;
using Acontplus.Core.Extensions;
using Acontplus.Notifications.Abstractions;
using Acontplus.Notifications.Models;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;
using Scriban;

namespace Acontplus.Notifications.Services;

public class MailKitService : IMailKitService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncRetryPolicy _authRetryPolicy;

    // A simple pool for SmtpClient instances
    private readonly ConcurrentBag<SmtpClient> _smtpClientPool;
    private readonly int _maxPoolSize;

    // Rate limiting for authentication attempts per server
    private readonly ConcurrentDictionary<string, DateTime> _lastAuthAttempt = new();
    private readonly ConcurrentDictionary<string, int> _authAttemptCount = new();
    private readonly TimeSpan _minAuthInterval;
    private readonly int _maxAuthAttemptsPerHour;

    public MailKitService(IConfiguration configuration, ILogger<MailKitService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configure retry policy for general SMTP operations (non-auth related)
        _retryPolicy = Policy
            .Handle<SmtpProtocolException>(ex =>
            {
                _logger.LogDebug(ex, "Caught SmtpProtocolException: {Message}", ex.Message);
                // Don't retry auth-related errors here - handle them separately
                return !ex.Message.Contains("too many login attempts", StringComparison.OrdinalIgnoreCase) &&
                       !ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) &&
                       (ex.Message.Contains("Service not available", StringComparison.OrdinalIgnoreCase) ||
                        ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase));
            })
            .Or<SmtpCommandException>(ex =>
            {
                _logger.LogDebug(ex, "Caught SmtpCommandException with StatusCode {StatusCode}: {Message}", ex.StatusCode, ex.Message);
                // Only retry 4xx errors that are not authentication related
                return (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500 &&
                       !ex.Message.Contains("too many login attempts", StringComparison.OrdinalIgnoreCase) &&
                       !ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase);
            })
            .Or<System.Net.Sockets.SocketException>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Attempt {RetryCount} failed for SMTP operation. Retrying in {TimeSpan}...", retryCount, timeSpan);
                });

        // Separate retry policy specifically for authentication with longer delays
        _authRetryPolicy = Policy
            .Handle<SmtpProtocolException>(ex =>
            {
                _logger.LogDebug(ex, "Caught authentication-related SmtpProtocolException: {Message}", ex.Message);
                return ex.Message.Contains("too many login attempts", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("authentication failed", StringComparison.OrdinalIgnoreCase);
            })
            .Or<SmtpCommandException>(ex =>
            {
                _logger.LogDebug(ex, "Caught authentication-related SmtpCommandException with StatusCode {StatusCode}: {Message}", ex.StatusCode, ex.Message);
                return ex.Message.Contains("too many login attempts", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("authentication failed", StringComparison.OrdinalIgnoreCase) ||
                       (int)ex.StatusCode == 535; // Authentication failed status code
            })
            .Or<MailKit.Security.AuthenticationException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMinutes(Math.Pow(2, retryAttempt)), // 2min, 4min delays
                onRetryAsync: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Authentication attempt {RetryCount} failed. Waiting {TimeSpan} before retry...", retryCount, timeSpan);
                    return Task.CompletedTask;
                });

        _maxPoolSize = _configuration.GetValue("MailKit:MaxPoolSize", 3); // Reduced default pool size
        _smtpClientPool = new ConcurrentBag<SmtpClient>();

        // Initialize rate limiting configuration
        _minAuthInterval = TimeSpan.FromSeconds(_configuration.GetValue("MailKit:MinAuthIntervalSeconds", 30));
        _maxAuthAttemptsPerHour = _configuration.GetValue("MailKit:MaxAuthAttemptsPerHour", 10);
    }

    private async Task<SmtpClient> GetConnectedSmtpClientAsync(EmailModel email, CancellationToken ct)
    {
        var serverKey = $"{email.SmtpServer}:{email.SmtpPort}:{email.SenderEmail}";

        // Check rate limiting before attempting authentication
        if (!CanAttemptAuthentication(serverKey))
        {
            var waitTime = GetAuthenticationWaitTime(serverKey);
            _logger.LogWarning("Rate limiting authentication attempts for {ServerKey}. Next attempt allowed in {WaitTime}",
                serverKey, waitTime);

            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, ct);
            }
        }

        // Try to reuse existing connection first
        if (_smtpClientPool.TryTake(out var client))
        {
            if (client.IsConnected && client.IsAuthenticated)
            {
                _logger.LogDebug("Reusing existing SMTP client from pool for {ServerKey}.", serverKey);
                return client;
            }
            else
            {
                _logger.LogDebug("SMTP client from pool was disconnected or unauthenticated. Disposing and creating new.");
                client.Dispose();
            }
        }

        // Create and authenticate new client with rate limiting
        return await _authRetryPolicy.ExecuteAsync(async () =>
        {
            var newClient = new SmtpClient();
            try
            {
                newClient.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                newClient.CheckCertificateRevocation = false;

                // Add timeout configurations
                newClient.Timeout = 30000; // 30 seconds timeout

                _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort}...", email.SmtpServer, email.SmtpPort);
                await newClient.ConnectAsync(email.SmtpServer, email.SmtpPort, MailKit.Security.SecureSocketOptions.Auto, ct);

                // Record authentication attempt
                RecordAuthenticationAttempt(serverKey);

                _logger.LogInformation("Authenticating with SMTP server for {SenderEmail}...", email.SenderEmail);
                await newClient.AuthenticateAsync(email.SenderEmail!, email.Password, ct);

                _logger.LogInformation("Successfully connected and authenticated to SMTP server.");

                // Reset auth attempt count on successful authentication
                _authAttemptCount.TryRemove(serverKey, out _);

                return newClient;
            }
            catch
            {
                newClient?.Dispose();
                throw;
            }
        });
    }

    private bool CanAttemptAuthentication(string serverKey)
    {
        var now = DateTime.UtcNow;

        // Check if we've exceeded attempts per hour
        if (_authAttemptCount.TryGetValue(serverKey, out var attempts) && attempts >= _maxAuthAttemptsPerHour)
        {
            if (_lastAuthAttempt.TryGetValue(serverKey, out var lastAttempt) &&
                now - lastAttempt < TimeSpan.FromHours(1))
            {
                return false;
            }
            // Reset counter if more than an hour has passed
            _authAttemptCount.TryRemove(serverKey, out _);
        }

        // Check minimum interval between attempts
        return !_lastAuthAttempt.TryGetValue(serverKey, out var lastAuth) ||
            now - lastAuth >= _minAuthInterval;
    }

    private TimeSpan GetAuthenticationWaitTime(string serverKey)
    {
        if (_lastAuthAttempt.TryGetValue(serverKey, out var lastAttempt))
        {
            var elapsed = DateTime.UtcNow - lastAttempt;
            var remaining = _minAuthInterval - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        return TimeSpan.Zero;
    }

    private void RecordAuthenticationAttempt(string serverKey)
    {
        var now = DateTime.UtcNow;
        _lastAuthAttempt.AddOrUpdate(serverKey, now, (key, oldValue) => now);
        _authAttemptCount.AddOrUpdate(serverKey, 1, (key, oldValue) => oldValue + 1);
    }

    private void ReturnSmtpClientToPool(SmtpClient client)
    {
        if (_smtpClientPool.Count < _maxPoolSize && client.IsConnected && client.IsAuthenticated)
        {
            _smtpClientPool.Add(client);
            _logger.LogDebug("Returned SMTP client to pool. Current pool size: {PoolSize}", _smtpClientPool.Count);
        }
        else
        {
            _logger.LogDebug("SMTP client pool is full or client is invalid. Disposing client.");
            try
            {
                if (client.IsConnected)
                    client.Disconnect(quit: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disconnecting SMTP client during disposal");
            }
            finally
            {
                client.Dispose();
            }
        }
    }

    public async Task<bool> SendAsync(EmailModel email, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(email.SenderEmail, nameof(email.SenderEmail));
        SmtpClient? smtpClient = null;

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                using var message = new MimeMessage();

                message.To.Clear();
                message.From.Add(new MailboxAddress(email.SenderName, email.SenderEmail!));
                message.Sender = new MailboxAddress(email.SenderName, email.SenderEmail!);

                var delimiters = new char[] { ',', ';', '|' };
                var receiver = email.RecipientEmail.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                foreach (string mailAddress in receiver)
                    message.To.Add(MailboxAddress.Parse(mailAddress.Trim()));

                if (!string.IsNullOrEmpty(email.Cc))
                {
                    var cc = email.Cc.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string mailAddress in cc)
                        message.Cc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }

                var body = new BodyBuilder();
                message.Subject = email.Subject;

                if (!email.IsHtml)
                {
                    var pathToHtmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", email.Template ?? string.Empty);
                    if (!File.Exists(pathToHtmlFile))
                    {
                        _logger.LogError("Email template file not found: {PathToHtmlFile}", pathToHtmlFile);
                        throw new FileNotFoundException($"Email template file not found: {pathToHtmlFile}");
                    }

                    var htmlString = await File.ReadAllTextAsync(pathToHtmlFile, ct);
                    var emailBody = ProcessTemplate(htmlString,
                        JsonExtensions.DeserializeOptimized<IDictionary<string, object>>(email.Body)!);

                    body.HtmlBody = emailBody;

                    var mediaImagesPath = _configuration.GetSection("Media").GetSection("Images").Value;
                    if (string.IsNullOrEmpty(mediaImagesPath))
                    {
                        _logger.LogWarning("Configuration 'Media:Images' is not set. Skipping logo embedding.");
                    }
                    else
                    {
                        var pathLogo = Path.Combine(mediaImagesPath, "Logos", email.Logo ?? string.Empty);
                        if (File.Exists(pathLogo))
                        {
                            var image = await body.LinkedResources.AddAsync(pathLogo, ct);
                            image.ContentId = MimeUtils.GenerateMessageId();
                            body.HtmlBody = body.HtmlBody.Replace("[img-logo]", $"cid:{image.ContentId}");
                        }
                        else
                        {
                            _logger.LogWarning("Email logo file not found: {PathLogo}", pathLogo);
                        }
                    }
                }
                else
                {
                    body.HtmlBody = email.Body;
                }

                if (email.Files is { Count: > 0 })
                {
                    foreach (var formFile in email.Files)
                    {
                        // Skip files with missing FileName or Content
                        if (string.IsNullOrEmpty(formFile.FileName) || formFile.Content == null)
                        {
                            _logger.LogWarning("Skipping attachment with missing FileName or Content");
                            continue;
                        }

                        var extension = Path.GetExtension(formFile.FileName)?.ToLowerInvariant();
                        switch (extension)
                        {
                            case ".pdf":
                                body.Attachments.Add(formFile.FileName, formFile.Content,
                                    MimeKit.ContentType.Parse(MediaTypeNames.Application.Pdf));
                                break;
                            case ".xml":
                                body.Attachments.Add(formFile.FileName, formFile.Content,
                                    MimeKit.ContentType.Parse(MediaTypeNames.Application.Xml));
                                break;
                            default:
                                body.Attachments.Add(formFile.FileName, formFile.Content,
                                    MimeKit.ContentType.Parse(MediaTypeNames.Application.Octet));
                                break;
                        }
                    }
                }

                message.Body = body.ToMessageBody();

                smtpClient = await GetConnectedSmtpClientAsync(email, ct);
                await smtpClient.SendAsync(message, ct);

                _logger.LogInformation("Email sent successfully to {RecipientEmail} with subject '{Subject}'.",
                    email.RecipientEmail, email.Subject);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {RecipientEmail} with subject '{Subject}'.",
                email.RecipientEmail, email.Subject);

            // Don't return failed clients to pool
            if (smtpClient != null)
            {
                try
                {
                    if (smtpClient.IsConnected)
                        smtpClient.Disconnect(quit: true, ct);
                }
                catch (Exception disconnectEx)
                {
                    _logger.LogDebug(disconnectEx, "Error disconnecting failed SMTP client");
                }
                finally
                {
                    smtpClient.Dispose();
                    smtpClient = null;
                }
            }
            throw;
        }
        finally
        {
            if (smtpClient != null && smtpClient.IsConnected && smtpClient.IsAuthenticated)
            {
                ReturnSmtpClientToPool(smtpClient);
            }
            else if (smtpClient != null)
            {
                smtpClient.Dispose();
            }
        }
    }

    private static string ProcessTemplate(string template, IDictionary<string, object> data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        var reportData = JsonExtensions.DeserializeOptimized<ExpandoObject>(JsonSerializer.Serialize(data, options));

        var scriptObject = new ScriptObject();
        foreach (var prop in reportData)
        {
            scriptObject.Add(prop.Key, prop.Value);
        }

        var templateTo = Template.Parse(template);
        return templateTo.Render(scriptObject, member => LowerFirstCharacter(member.Name));
    }

    private static string LowerFirstCharacter(string value)
    {
        return value.Length > 1 ? char.ToLower(value[0]) + value.Substring(1) : value;
    }

    public void Dispose()
    {
        while (_smtpClientPool.TryTake(out var client))
        {
            try
            {
                if (client.IsConnected)
                    client.Disconnect(quit: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disconnecting SMTP client during disposal");
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
