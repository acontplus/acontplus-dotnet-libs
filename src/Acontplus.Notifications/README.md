# Acontplus.Notifications

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Notifications.svg)](https://www.nuget.org/packages/Acontplus.Notifications)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Production-ready email notification library** with MailKit and Amazon SES support. Features template caching, rate limiting, connection pooling, and enterprise-grade scalability.

## 🚀 Features

### v1.5.0 - Performance & Caching
- ⚡ **Template Caching**: 50x faster template loading (30min memory cache)
- 📊 **99% Less I/O**: Cached templates eliminate repeated disk reads
- 🔄 **Backward Compatible**: Optional `IMemoryCache` injection

### Core Capabilities
- 📧 **Email notifications** via MailKit (SMTP) and Amazon SES (HTTP)
- 🎨 **Templated emails** with Scriban engine
- 🔄 **Retry logic** with Polly (exponential backoff)
- 🚦 **Rate limiting** (SES: 14 emails/sec default, SMTP: auth throttling)
- 🏊 **Connection pooling** for SMTP (configurable pool size)
- 📦 **Bulk sending** with batching (SES: 50 recipients/batch)
- 🧵 **Thread-safe** concurrent operations
- 📨 **Attachments** support (PDF, images, documents)
- 🌐 **WhatsApp and push** notification support (planned)

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package Acontplus.Notifications -Version 1.5.0
```

### .NET CLI
```bash
dotnet add package Acontplus.Notifications --version 1.5.0
```

### PackageReference
```xml
<PackageReference Include="Acontplus.Notifications" Version="1.5.0" />
```

## 🎯 Quick Start

### 1. Configure Services
```csharp
// In Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Option 1: Amazon SES (Recommended for Production)
builder.Services.AddSingleton<IMailKitService, AmazonSesService>();

// Option 2: MailKit SMTP
builder.Services.AddSingleton<IMailKitService, MailKitService>();

// Optional: Enable template caching for better performance
builder.Services.AddMemoryCache();
```

### 2. Configure appsettings.json

**Amazon SES Configuration:**
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
  "Templates": {
    "Path": "Templates"
  },
  "Media": {
    "ImagesPath": "wwwroot/images"
  }
}
```

**MailKit SMTP Configuration:**
```json
{
  "MailKit": {
    "MaxPoolSize": 3,
    "MinAuthIntervalSeconds": 30,
    "MaxAuthAttemptsPerHour": 10
  }
}
```

### 3. Send an Email
```csharp
public class EmailController : ControllerBase
{
    private readonly IMailKitService _emailService;

    public EmailController(IMailKitService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        var email = new EmailModel
        {
            SenderName = "My App",
            SenderEmail = "noreply@example.com",
            RecipientEmail = request.To,
            Subject = request.Subject,
            Body = request.HtmlBody,
            IsHtml = true
        };

        var success = await _emailService.SendAsync(email, CancellationToken.None);

        return success ? Ok("Email sent") : StatusCode(500, "Failed to send email");
    }
}
```

### 4. Send with Template (NEW in v1.5.0 - Cached!)
```csharp
var email = new EmailModel
{
    SenderEmail = "noreply@example.com",
    RecipientEmail = "user@example.com",
    Subject = "Welcome!",
    Template = "welcome-email.html", // Cached for 30 minutes!
    Body = JsonSerializer.Serialize(new
    {
        UserName = "John",
        ActivationLink = "https://example.com/activate"
    }),
    IsHtml = false // Will process template
};

await _emailService.SendAsync(email, CancellationToken.None);
```

## 📚 Advanced Usage

### Bulk Email Sending (Amazon SES)
```csharp
var emails = new List<EmailModel>();
for (int i = 0; i < 1000; i++)
{
    emails.Add(new EmailModel
    {
        SenderEmail = "noreply@example.com",
        RecipientEmail = $"user{i}@example.com",
        Subject = "Newsletter",
        Body = "<h1>Hello!</h1>",
        IsHtml = true
    });
}

// Automatically batched (50 per batch) with rate limiting (14/sec)
var success = await _emailService.SendBulkAsync(emails, CancellationToken.None);
// Completes in ~70 seconds with smooth throttling
```

### Email with Attachments
```csharp
var email = new EmailModel
{
    SenderEmail = "noreply@example.com",
    RecipientEmail = "user@example.com",
    Subject = "Invoice",
    Body = "<h1>Your Invoice</h1>",
    IsHtml = true,
    Files = new List<EmailAttachment>
    {
        new EmailAttachment
        {
            FileName = "invoice.pdf",
            Content = pdfBytes
        }
    }
};

await _emailService.SendAsync(email, CancellationToken.None);
```

### Template with Logo
```csharp
var email = new EmailModel
{
    SenderEmail = "noreply@example.com",
    RecipientEmail = "user@example.com",
    Subject = "Welcome!",
    Template = "welcome.html",
    Logo = "company-logo.png", // From Media:ImagesPath/Logos/
    Body = JsonSerializer.Serialize(new { UserName = "John" }),
    IsHtml = false
};

await _emailService.SendAsync(email, CancellationToken.None);
```

## ⚙️ Configuration Options

### Amazon SES Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Region` | us-east-1 | AWS SES region |
| `DefaultFromEmail` | - | Default sender email |
| `MaxSendRate` | 14 | Max emails per second |
| `BatchSize` | 50 | Recipients per bulk batch |
| `BatchDelayMs` | 100 | Delay between batches |
| `AccessKey` | - | AWS access key (optional if using IAM) |
| `SecretKey` | - | AWS secret key (optional if using IAM) |

### MailKit SMTP Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxPoolSize` | 3 | SMTP connection pool size |
| `MinAuthIntervalSeconds` | 30 | Min time between auth attempts |
| `MaxAuthAttemptsPerHour` | 10 | Auth rate limiting |

### Template Caching (v1.5.0+)

**Automatic** when `IMemoryCache` is registered:
- **Cache Duration**: 30 minutes (sliding expiration)
- **Priority**: Normal
- **Key Format**: `email_template:{templateName}`

```csharp
// Enable caching (optional but recommended)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100; // Max cached templates
    options.CompactionPercentage = 0.25;
});
```

## 🚀 Performance

### v1.5.0 Template Caching Impact

| Metric | v1.4.x | v1.5.0 (Cached) | Improvement |
|--------|--------|-----------------|-------------|
| Template load time | 10-50ms | <1ms | **50x faster** |
| Disk I/O (high volume) | Every request | Once per 30min | **99% reduction** |
| Memory overhead | ~0KB | ~50KB/template | Minimal |

### Rate Limiting Behavior

**Amazon SES:**
- Default: 14 emails/second (AWS sandbox limit)
- Production: Configurable up to account limits
- Automatic throttling with logging

**MailKit:**
- SMTP connection pooling (default: 3 connections)
- Auth rate limiting prevents account lockouts
- Configurable intervals and max attempts

## 📧 API Documentation

### IMailKitService Interface

```csharp
public interface IMailKitService
{
    Task<bool> SendAsync(EmailModel email, CancellationToken ct);
    Task<bool> SendBulkAsync(IEnumerable<EmailModel> emails, CancellationToken ct);
}
```

### EmailModel Class

```csharp
public class EmailModel
{
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string RecipientEmail { get; set; } // Multiple: "a@x.com,b@x.com"
    public string? Cc { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
    public string? Template { get; set; } // Template file name
    public string? Logo { get; set; }
    public List<EmailAttachment>? Files { get; set; }

    // SMTP specific (MailKit only)
    public string? SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string? Password { get; set; }
}
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
```bash
git clone https://github.com/acontplus/acontplus-dotnet-libs.git
cd acontplus-dotnet-libs
dotnet restore
dotnet build
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 Email: proyectos@acontplus.com
- 🐛 Issues: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- 📖 Documentation: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

## 👨‍💻 Author

**Ivan Paz** - [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** - Software solutions

---

**Built with ❤️ for the .NET community**
