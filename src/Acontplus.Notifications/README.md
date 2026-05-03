# Acontplus.Notifications

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Notifications.svg)](https://www.nuget.org/packages/Acontplus.Notifications)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Production-ready multi-channel notification library** for .NET 10. Covers email (MailKit SMTP + Amazon SES) and **WhatsApp Cloud API** (Meta Graph API v23.0) in a single package — templating, media, interactive messages, multi-tenant credentials, resilience, and webhook validation included.

## 🚀 Features

### WhatsApp Cloud API (Meta Graph API v23.0)
- 💬 **Text messages** — plain text with URL preview, in-thread replies
- 📋 **Templates** — body/header params, image/video/document headers, quick-reply & URL buttons
- 🖼️ **Media** — image, document, audio, video, sticker (URL or pre-uploaded ID)
- 📍 **Location** — map pin with name and address
- 🔘 **Interactive** — quick-reply buttons, scrollable list menu, CTA URL button
- 👍 **Reactions** — send/remove emoji reactions on received messages
- ✅ **Read receipts** — mark messages as read (double blue ticks)
- 📤 **Media upload** — upload files once, reuse the ID in multiple messages
- 🔐 **Webhook validation** — HMAC-SHA256 signature verification (X-Hub-Signature-256)
- 🏢 **Multi-tenant** — default account + named accounts per company + per-request inline override
- 🔄 **Built-in resilience** — 3 retries, exponential back-off, circuit breaker, per-attempt timeout

### Core Email Capabilities
- 📧 **Email** via MailKit (SMTP) and Amazon SES (HTTP v2)
- 🎨 **Scriban templating** with logo embedding and variable interpolation
- 🔄 **Retry logic** with Polly (exponential back-off, jitter)
- 🚦 **Rate limiting** (SES: 14 emails/sec default; SMTP: auth throttling)
- 🏊 **SMTP connection pooling** (configurable pool size)
- 📦 **Bulk sending** with batching (SES: 50 recipients/batch)
- 🧵 **Thread-safe** concurrent operations
- 📨 **Attachments** (PDF, images, documents)

## 📦 Installation

```bash
dotnet add package Acontplus.Notifications
```

```xml
<PackageReference Include="Acontplus.Notifications" Version="1.6.2" />
```

---

## ⚡ WhatsApp Cloud API (v1.6.0)

### 1. Register the service

```csharp
// Program.cs
builder.Services.AddWhatsAppService(builder.Configuration);
// or inline:
builder.Services.AddWhatsAppService(opts =>
{
    opts.PhoneNumberId  = "1234567890";
    opts.AccessToken    = "EAAxxxx...";
    opts.ApiVersion     = "v23.0";
    opts.DefaultCountryCode = "593"; // 09xxxxxxxx → 593 9xxxxxxxx
});
```

### 2. appsettings.json

```json
{
  "WhatsApp": {
    // REQUIRED (when using server-stored credentials)
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID",
    "AccessToken": "YOUR_PERMANENT_SYSTEM_USER_TOKEN",

    // OPTIONAL — defaults shown
    "ApiVersion": "v23.0",
    "TimeoutSeconds": 30,

    // OPTIONAL — auto-prefix numbers starting with "0" (e.g. 09xxxxxxxx → 5939xxxxxxxx)
    "DefaultCountryCode": "593",

    // OPTIONAL — only needed if you call ValidateWebhookSignature()
    "AppSecret": "YOUR_APP_SECRET_FOR_WEBHOOK_VALIDATION",

    // OPTIONAL — only needed if you handle Meta's GET webhook verify handshake
    "WebhookVerifyToken": "YOUR_WEBHOOK_VERIFY_TOKEN",

    // OPTIONAL — multi-tenant: named accounts override the default credentials above
    "Accounts": {
      "company-a": {
        "PhoneNumberId": "COMPANY_A_PHONE_ID",
        "AccessToken": "COMPANY_A_TOKEN"
      }
    }
  }
}
```

> **Per-request credentials:** If your callers supply `AccessToken` / `PhoneNumberId` on every request
> (e.g. credentials come from the frontend), you can omit `PhoneNumberId` and `AccessToken` entirely
> from appsettings and only keep `TimeoutSeconds`.
>
> **Obtain credentials:** Meta Business Manager → WhatsApp → API Setup.
> Use a **permanent system-user token** (never expires) instead of a temporary one.

### 3. Inject and send

```csharp
public class NotificationService(IWhatsAppService whatsApp)
{
    // Text message (requires 24-hour conversation window)
    public Task<WhatsAppResult> SendTextAsync(string to, string body) =>
        whatsApp.SendTextAsync(new() { To = to, Body = body });

    // Template (works any time — no 24-hour window needed)
    public Task<WhatsAppResult> SendInvoiceAsync(string to, string mediaId, string customerName, string invoiceNumber) =>
        whatsApp.SendTemplateAsync(new()
        {
            To            = to,
            TemplateName  = "notificacion_documento",
            LanguageCode  = "es_EC",
            Components    =
            [
                WhatsAppTemplateComponent.HeaderDocument(mediaId, "factura.pdf", isId: true),
                WhatsAppTemplateComponent.Body(customerName, invoiceNumber)
            ]
        });

    // Upload a file once → reuse the ID
    public async Task<string?> UploadInvoiceAsync(Stream pdfStream) =>
        await whatsApp.UploadMediaAsync(new()
        {
            FileStream  = pdfStream,
            FileName    = "invoice.pdf",
            ContentType = "application/pdf"
        });

    // Interactive quick-reply buttons
    public Task<WhatsAppResult> SendConfirmationAsync(string to) =>
        whatsApp.SendInteractiveAsync(new()
        {
            To              = to,
            InteractiveType = WhatsAppInteractiveType.Button,
            BodyText        = "Do you confirm your appointment?",
            ReplyButtons    =
            [
                new WhatsAppReplyButton { Id = "yes", Title = "Yes, confirm" },
                new WhatsAppReplyButton { Id = "no",  Title = "No, cancel"  }
            ]
        });

    // Multi-tenant: named account
    public Task<WhatsAppResult> SendForCompanyAsync(string to, string message) =>
        whatsApp.SendTextAsync(new()
        {
            To          = to,
            Body        = message,
            Credentials = WhatsAppCredentials.FromAccount("company-a")
        });
}
```

### 4. Webhook validation

```csharp
app.MapPost("/webhook/whatsapp", async (
    HttpRequest req,
    IWhatsAppService whatsApp) =>
{
    var signature = req.Headers["X-Hub-Signature-256"].ToString();
    var body      = await new StreamReader(req.Body).ReadToEndAsync();

    if (!whatsApp.ValidateWebhookSignature(signature, body))
        return Results.Unauthorized();

    // process payload ...
    return Results.Ok();
});

// Webhook verification handshake (GET)
app.MapGet("/webhook/whatsapp", (
    [FromQuery(Name = "hub.mode")]        string mode,
    [FromQuery(Name = "hub.verify_token")] string token,
    [FromQuery(Name = "hub.challenge")]    string challenge,
    IConfiguration config) =>
{
    var expected = config["WhatsApp:WebhookVerifyToken"];
    return mode == "subscribe" && token == expected
        ? Results.Ok(int.Parse(challenge))
        : Results.Unauthorized();
});
```

### Supported message types

| Method | Type | 24-h window? |
|--------|------|:---:|
| `SendTextAsync` | Plain text with optional preview | Required |
| `SendTemplateAsync` | Pre-approved template (text + media header, buttons) | Not required |
| `SendMediaAsync` | Image / Document / Audio / Video / Sticker | Required |
| `SendLocationAsync` | Map pin | Required |
| `SendInteractiveAsync` | Quick-reply buttons / List menu / CTA URL | Required |
| `SendReactionAsync` | Emoji reaction on a received message | Required |
| `MarkAsReadAsync` | Read receipt (double blue ticks) | — |
| `UploadMediaAsync` | Upload file → get reusable media ID | — |
| `ValidateWebhookSignature` | HMAC-SHA256 webhook verification | — |

### Multi-tenant credential resolution order

1. **Named account** — `WhatsAppCredentials.FromAccount("name")` → looks up `WhatsApp:Accounts:name`
2. **Inline override** — `WhatsAppCredentials.Inline(phoneId, token)`
3. **Default** — top-level `WhatsApp:PhoneNumberId` + `WhatsApp:AccessToken`

---

## 📧 Email (MailKit / Amazon SES)

### Quick Start

```csharp
// Program.cs
builder.Services.AddMemoryCache(); // enables template caching
builder.Services.AddSingleton<IMailKitService, AmazonSesService>(); // or MailKitService
```

### appsettings.json

```json
{
  "AWS": {
    "SES": {
      "Region": "us-east-1",
      "DefaultFromEmail": "noreply@example.com",
      "MaxSendRate": 14,
      "BatchSize": 50,
      "BatchDelayMs": 100,
      "AccessKey": "AKIA...",
      "SecretKey": "your-secret-key"
    }
  },
  "Templates": { "Path": "Templates" },
  "Media": { "ImagesPath": "wwwroot/images" }
}
```

### Send an email

```csharp
var success = await emailService.SendAsync(new EmailModel
{
    SenderEmail    = "noreply@example.com",
    RecipientEmail = "user@example.com",
    Subject        = "Welcome!",
    Template       = "welcome.html", // cached 30 min
    Body           = JsonSerializer.Serialize(new { UserName = "John" }),
    IsHtml         = false
});
```

### Bulk sending

```csharp
var emails = recipients.Select(r => new EmailModel { ... }).ToList();
await emailService.SendBulkAsync(emails, ct); // auto-batched, rate-limited
```

---

## ⚙️ Configuration Reference

### WhatsApp settings

| Key | Default | Required? | Description |
|-----|---------|-----------|-------------|
| `PhoneNumberId` | — | Conditional ¹ | WhatsApp Business phone number ID |
| `AccessToken` | — | Conditional ¹ | Meta Graph API token (system-user recommended) |
| `ApiVersion` | `v23.0` | Optional | Meta Graph API version |
| `TimeoutSeconds` | `30` | Optional | Per-attempt HTTP timeout |
| `DefaultCountryCode` | `null` | Optional | Auto-prefix numbers starting with `0` (e.g. `"593"`) |
| `AppSecret` | `null` | Optional ² | App secret for webhook HMAC-SHA256 signature validation |
| `WebhookVerifyToken` | `null` | Optional ³ | Token for Meta's GET webhook verify handshake |
| `Accounts` | `{}` | Optional | Named accounts for multi-tenant (key → `PhoneNumberId` + `AccessToken`) |

> ¹ **Conditional** — required when using server-stored default credentials. Omit when all callers
> supply credentials per-request via `WhatsAppCredentials` (e.g. credentials come from the frontend).
> In that case only `TimeoutSeconds` is meaningful.
>
> ² Only needed if you call `ValidateWebhookSignature()`.
>
> ³ Only needed if you handle Meta's GET webhook verify handshake endpoint.

### Amazon SES settings

| Key | Default | Description |
|-----|---------|-------------|
| `Region` | `us-east-1` | AWS SES region |
| `DefaultFromEmail` | — | Default sender |
| `MaxSendRate` | `14` | Max emails/second |
| `BatchSize` | `50` | Recipients per bulk batch |
| `BatchDelayMs` | `100` | Delay between batches |

### MailKit SMTP settings

| Key | Default | Description |
|-----|---------|-------------|
| `MaxPoolSize` | `3` | SMTP connection pool size |
| `MinAuthIntervalSeconds` | `30` | Min time between auth attempts |
| `MaxAuthAttemptsPerHour` | `10` | Auth rate limit |

---

## 🚀 Performance

### WhatsApp resilience (v1.6.0)

| Layer | Configuration |
|-------|--------------|
| Retries | 3 attempts, 600 ms base delay (exponential) |
| Per-attempt timeout | 30 s |
| Total timeout | 90 s (all retries included) |
| Circuit breaker | Included via `AddStandardResilienceHandler` |

### Email template caching (v1.5.0+)

| Metric | Without cache | With cache | Improvement |
|--------|--------------|------------|-------------|
| Template load time | 10–50 ms | <1 ms | 50× faster |
| Disk I/O (high volume) | Every request | Once per 30 min | 99% reduction |

---

## 📚 API Reference

### IWhatsAppService

```csharp
Task<WhatsAppResult>  SendTextAsync(SendWhatsAppTextRequest, CancellationToken);
Task<WhatsAppResult>  SendTemplateAsync(SendWhatsAppTemplateRequest, CancellationToken);
Task<WhatsAppResult>  SendMediaAsync(SendWhatsAppMediaRequest, CancellationToken);
Task<WhatsAppResult>  SendLocationAsync(SendWhatsAppLocationRequest, CancellationToken);
Task<WhatsAppResult>  SendInteractiveAsync(SendWhatsAppInteractiveRequest, CancellationToken);
Task<WhatsAppResult>  SendReactionAsync(SendWhatsAppReactionRequest, CancellationToken);
Task<bool>            MarkAsReadAsync(string messageId, WhatsAppCredentials?, CancellationToken);
Task<string?>         UploadMediaAsync(WhatsAppMediaUpload, WhatsAppCredentials?, CancellationToken);
bool                  ValidateWebhookSignature(string xHubSignature256, string rawBody, string? appSecret);
```

### IMailKitService

```csharp
Task<bool> SendAsync(EmailModel email, CancellationToken ct);
Task<bool> SendBulkAsync(IEnumerable<EmailModel> emails, CancellationToken ct);
```
