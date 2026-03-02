# Acontplus.Reports

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Reports.svg)](https://www.nuget.org/packages/Acontplus.Reports)

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance .NET library for RDLC report generation and direct printing with advanced features for enterprise applications. Optimized for high concurrency, large reports, thermal printers, and production workloads.

## 🚀 Features

### Core Capabilities

- ✅ **Async/Await Support** - Fully asynchronous API for better scalability
- ✅ **RDLC Report Generation** - Support for PDF, Excel, Word, HTML5, Image exports
- ✅ **QuestPDF Dynamic PDF** - Fluent code-first PDF generation without design-time files
- ✅ **Direct Printing** - Print reports directly to thermal/receipt printers
- ✅ **High Concurrency** - Built-in concurrency limiting and thread-safe operations
- ✅ **Memory Optimization** - Stream pooling and efficient memory management for large reports
- ✅ **Smart Caching** - Configurable report definition caching with TTL and size limits
- ✅ **Comprehensive Logging** - Structured logging with performance metrics
- ✅ **Error Handling** - Custom exceptions with detailed error context
- ✅ **Dependency Injection** - Full DI support with extension methods
- ✅ **Timeout Protection** - Configurable timeouts to prevent runaway report generation
- ✅ **Size Limits** - Configurable maximum report sizes

### Performance Optimizations

- Report definition caching reduces file I/O
- Concurrency limiting prevents resource exhaustion
- Async operations improve scalability under load
- Memory pooling reduces GC pressure
- Cancellation token support for graceful shutdowns
- Separate semaphore controls for report generation and printing

## 📦 Installation

### NuGet Package Manager

```bash
Install-Package Acontplus.Reports
```

### .NET CLI

```bash
dotnet add package Acontplus.Reports
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Acontplus.Reports" Version="1.6.0" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Register Services

```csharp
using Acontplus.Reports.Extensions;

// In Program.cs or Startup.cs
builder.Services.AddReportServices(builder.Configuration);

// Or with custom configuration
builder.Services.AddReportServices(options =>
{
    options.MainDirectory = "Reports";
    options.MaxConcurrentReports = 20;
    options.MaxConcurrentPrintJobs = 5;
    options.MaxReportSizeBytes = 50 * 1024 * 1024; // 50 MB
    options.ReportGenerationTimeoutSeconds = 180; // 3 minutes
    options.PrintJobTimeoutSeconds = 120; // 2 minutes
    options.EnableReportDefinitionCache = true;
    options.MaxCachedReportDefinitions = 100;
    options.CacheTtlMinutes = 60;
    options.EnableDetailedLogging = true;
});
```

### 2. Configure appsettings.json

```json
{
  "Reports": {
    "MainDirectory": "Reports",
    "ExternalDirectory": "C:\\ExternalReports",
    "MaxReportSizeBytes": 104857600,
    "ReportGenerationTimeoutSeconds": 300,
    "PrintJobTimeoutSeconds": 180,
    "EnableReportDefinitionCache": true,
    "MaxCachedReportDefinitions": 100,
    "CacheTtlMinutes": 60,
    "EnableMemoryPooling": true,
    "MaxConcurrentReports": 10,
    "MaxConcurrentPrintJobs": 5,
    "EnableDetailedLogging": false
  }
}
```

### 3. Add Report Files

Ensure your RDLC files are included in your project:

```xml
<ItemGroup>
    <None Update="Reports\**\*.rdlc">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Reports\Images\**">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

## 📖 Comprehensive Usage Guide

### Report Generation Service

The `IRdlcReportService` provides methods for generating reports in various formats.

#### Complete Example - Invoice Report

```csharp
using Acontplus.Reports.Interfaces;
using Acontplus.Reports.DTOs;
using Acontplus.Reports.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IRdlcReportService _reportService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IRdlcReportService reportService,
        ILogger<InvoiceController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetInvoice(
        int invoiceId,
        [FromQuery] string format = "PDF",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Create Parameters DataSet
            var parameters = new DataSet();

            // Required: ReportProps table defines report location and format
            var reportPropsTable = new DataTable("ReportProps");
            reportPropsTable.Columns.Add("ReportPath", typeof(string));
            reportPropsTable.Columns.Add("ReportName", typeof(string));
            reportPropsTable.Columns.Add("ReportFormat", typeof(string));
            reportPropsTable.Rows.Add("InvoiceReport.rdlc", $"Invoice_{invoiceId}", format.ToUpper());
            parameters.Tables.Add(reportPropsTable);

            // Optional: ReportParams table for images, logos, and text parameters
            var reportParamsTable = new DataTable("ReportParams");
            reportParamsTable.Columns.Add("paramName", typeof(string));
            reportParamsTable.Columns.Add("paramValue", typeof(byte[]));
            reportParamsTable.Columns.Add("isPicture", typeof(bool));
            reportParamsTable.Columns.Add("isCompressed", typeof(bool));

            // Add text parameters
            reportParamsTable.Rows.Add(
                "CompanyName",
                System.Text.Encoding.UTF8.GetBytes("Acontplus Corporation"),
                false,
                false);

            reportParamsTable.Rows.Add(
                "InvoiceTitle",
                System.Text.Encoding.UTF8.GetBytes("COMMERCIAL INVOICE"),
                false,
                false);

            // Add logo/image parameter
            var logoBytes = await System.IO.File.ReadAllBytesAsync("wwwroot/images/logo.png");
            reportParamsTable.Rows.Add("CompanyLogo", logoBytes, true, false);

            parameters.Tables.Add(reportParamsTable);

            // Step 2: Create Data DataSet with your business data
            var data = new DataSet();

            // Invoice Header
            var headerTable = new DataTable("InvoiceHeader");
            headerTable.Columns.Add("InvoiceNumber", typeof(string));
            headerTable.Columns.Add("InvoiceDate", typeof(DateTime));
            headerTable.Columns.Add("CustomerName", typeof(string));
            headerTable.Columns.Add("CustomerAddress", typeof(string));
            headerTable.Columns.Add("Subtotal", typeof(decimal));
            headerTable.Columns.Add("Tax", typeof(decimal));
            headerTable.Columns.Add("Total", typeof(decimal));

            // Fetch from database (example)
            var invoice = await GetInvoiceFromDatabaseAsync(invoiceId);
            headerTable.Rows.Add(
                invoice.Number,
                invoice.Date,
                invoice.CustomerName,
                invoice.CustomerAddress,
                invoice.Subtotal,
                invoice.Tax,
                invoice.Total);
            data.Tables.Add(headerTable);

            // Invoice Items
            var itemsTable = new DataTable("InvoiceItems");
            itemsTable.Columns.Add("LineNumber", typeof(int));
            itemsTable.Columns.Add("ProductCode", typeof(string));
            itemsTable.Columns.Add("Description", typeof(string));
            itemsTable.Columns.Add("Quantity", typeof(decimal));
            itemsTable.Columns.Add("UnitPrice", typeof(decimal));
            itemsTable.Columns.Add("Amount", typeof(decimal));

            foreach (var item in invoice.Items)
            {
                itemsTable.Rows.Add(
                    item.LineNumber,
                    item.ProductCode,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.Amount);
            }
            data.Tables.Add(itemsTable);

            // Step 3: Generate the report
            var report = await _reportService.GetReportAsync(
                parameters,
                data,
                externalDirectory: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Generated invoice {InvoiceId} in {Format} format, size: {Size} bytes",
                invoiceId, format, report.FileContents.Length);

            // Step 4: Return the file
            return File(
                report.FileContents,
                report.ContentType,
                report.FileDownloadName);
        }
        catch (ReportNotFoundException ex)
        {
            _logger.LogError(ex, "Report template not found for invoice {InvoiceId}", invoiceId);
            return NotFound($"Report template not found: {ex.Message}");
        }
        catch (ReportTimeoutException ex)
        {
            _logger.LogWarning(ex, "Report generation timed out for invoice {InvoiceId}", invoiceId);
            return StatusCode(504, "Report generation timed out");
        }
        catch (ReportSizeExceededException ex)
        {
            _logger.LogWarning(ex, "Report size exceeded limit for invoice {InvoiceId}", invoiceId);
            return StatusCode(413, $"Report too large: {ex.ReportSize} bytes (max: {ex.MaxSize})");
        }
        catch (ReportGenerationException ex)
        {
            _logger.LogError(ex, "Report generation failed for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, $"Report generation failed: {ex.Message}");
        }
    }
}
```

### Printer Service

The `IRdlcPrinterService` enables direct printing to thermal printers, receipt printers, or standard printers.

#### Complete Example - Receipt Printing

```csharp
using Acontplus.Reports.Interfaces;
using Acontplus.Reports.DTOs;
using Acontplus.Reports.Exceptions;

public class PosService
{
    private readonly IRdlcPrinterService _printerService;
    private readonly ILogger<PosService> _logger;

    public PosService(IRdlcPrinterService printerService, ILogger<PosService> logger)
    {
        _printerService = printerService;
        _logger = logger;
    }

    public async Task<bool> PrintReceiptAsync(
        SaleTransaction sale,
        string printerName = "ThermalPrinter80mm",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Configure printer settings
            var printerConfig = new RdlcPrinterDto
            {
                // Printer name from Windows printer settings
                PrinterName = printerName,

                // RDLC file name (in Reports directory)
                FileName = "Receipt.rdlc",

                // Format for printing (IMAGE for thermal printers)
                Format = "IMAGE",

                // Directory paths
                ReportsDirectory = Path.Combine(AppContext.BaseDirectory, "Reports"),
                LogoDirectory = Path.Combine(AppContext.BaseDirectory, "Reports", "Images"),

                // Logo file name (without extension)
                LogoName = "store_logo",

                // Device info for thermal printer (EMF format)
                DeviceInfo = "<DeviceInfo><OutputFormat>EMF</OutputFormat></DeviceInfo>",

                // Number of copies
                Copies = 1
            };

            // Step 2: Prepare data sources
            var dataSources = new Dictionary<string, List<Dictionary<string, string>>>
            {
                // Receipt header data
                ["ReceiptHeader"] = new()
                {
                    new()
                    {
                        ["ReceiptNumber"] = sale.ReceiptNumber,
                        ["TransactionDate"] = sale.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["CashierName"] = sale.CashierName,
                        ["StoreName"] = "Acontplus Store",
                        ["StoreAddress"] = "123 Main St, City, State",
                        ["StoreTaxId"] = "TAX-123456789"
                    }
                },

                // Receipt items
                ["ReceiptItems"] = sale.Items.Select((item, index) => new Dictionary<string, string>
                {
                    ["LineNumber"] = (index + 1).ToString(),
                    ["ProductName"] = item.ProductName,
                    ["Quantity"] = item.Quantity.ToString("F2"),
                    ["UnitPrice"] = item.UnitPrice.ToString("F2"),
                    ["Total"] = item.Total.ToString("F2")
                }).ToList(),

                // Receipt totals
                ["ReceiptTotals"] = new()
                {
                    new()
                    {
                        ["Subtotal"] = sale.Subtotal.ToString("F2"),
                        ["Tax"] = sale.Tax.ToString("F2"),
                        ["Discount"] = sale.Discount.ToString("F2"),
                        ["Total"] = sale.Total.ToString("F2"),
                        ["PaymentMethod"] = sale.PaymentMethod,
                        ["AmountPaid"] = sale.AmountPaid.ToString("F2"),
                        ["Change"] = sale.Change.ToString("F2")
                    }
                }
            };

            // Step 3: Add report parameters
            var reportParams = new Dictionary<string, string>
            {
                ["Barcode"] = sale.ReceiptNumber,
                ["FooterMessage"] = "Thank you for your purchase!",
                ["CustomerName"] = sale.CustomerName ?? "Guest"
            };

            // Step 4: Create print request
            var printRequest = new RdlcPrintRequestDto
            {
                DataSources = dataSources,
                ReportParams = reportParams
            };

            // Step 5: Execute print job
            var success = await _printerService.PrintAsync(
                printerConfig,
                printRequest,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation(
                    "Receipt {ReceiptNumber} printed successfully to {PrinterName}",
                    sale.ReceiptNumber, printerName);
            }

            return success;
        }
        catch (ReportNotFoundException ex)
        {
            _logger.LogError(ex, "Receipt template not found");
            throw;
        }
        catch (ReportTimeoutException ex)
        {
            _logger.LogWarning(ex, "Print job timed out after {Timeout}s", ex.TimeoutSeconds);
            throw;
        }
        catch (ReportGenerationException ex)
        {
            _logger.LogError(ex, "Print job failed: {Message}", ex.Message);
            throw;
        }
    }
}
```

### Multi-Format Report Generation

Generate the same report in different formats:

```csharp
public async Task<Dictionary<string, byte[]>> GenerateMultiFormatReport(
    DataSet parameters,
    DataSet data,
    CancellationToken cancellationToken)
{
    var formats = new[] { "PDF", "EXCEL", "EXCELOPENXML", "WORDOPENXML", "HTML5" };
    var results = new Dictionary<string, byte[]>();

    foreach (var format in formats)
    {
        // Update format in parameters
        var reportPropsRow = parameters.Tables["ReportProps"]!.Rows[0];
        reportPropsRow["ReportFormat"] = format;

        var report = await _reportService.GetReportAsync(
            parameters,
            data,
            externalDirectory: false,
            cancellationToken);

        results[format] = report.FileContents;
    }

    return results;
}
```

## 🎨 QuestPDF Dynamic PDF Generation

`IQuestPdfReportService` provides code-first, fluent PDF generation via **QuestPDF 2026.x** — no RDLC templates, no design-time files. Reports are programmatically composed from typed request objects and rendered on any platform (.NET 10 / Linux / Windows / macOS).

### QuestPDF License

QuestPDF uses a tiered licensing model. The **Community** tier is free for projects with ≤ $1 M USD annual revenue and is the default. Set the tier once via `ReportOptions`:

```json
{
  "Reports": {
    "QuestPdfLicenseType": "Community"
  }
}
```

Or in code:

```csharp
builder.Services.AddReportServices(options =>
{
    options.QuestPdfLicenseType = QuestPdfLicenseType.Community; // Community | Professional | Enterprise
});
```

### Registration

```csharp
// Option A — register both RDLC and QuestPDF together (recommended)
builder.Services.AddReportServices(builder.Configuration);

// Option B — register QuestPDF only (no RDLC dependency)
builder.Services.AddQuestPdfReportService(builder.Configuration);

// Option C — QuestPDF only, fluent options
builder.Services.AddQuestPdfReportService(options =>
{
    options.QuestPdfLicenseType     = QuestPdfLicenseType.Community;
    options.MaxConcurrentReports    = 20;
    options.ReportGenerationTimeoutSeconds = 120;
    options.EnableDetailedLogging   = true;
});
```

---

### Quick Start — Single DataTable

```csharp
[ApiController]
[Route("api/[controller]")]
public class SalesReportController : ControllerBase
{
    private readonly IQuestPdfReportService _pdf;

    public SalesReportController(IQuestPdfReportService pdf) => _pdf = pdf;

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(CancellationToken ct)
    {
        var dt = await GetSalesDataAsync(); // Returns DataTable

        // Minimal usage — auto-derives columns from DataTable schema
        var response = await _pdf.GenerateFromDataTableAsync("Sales Report", dt, cancellationToken: ct);

        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }
}
```

---

### Full Example — Multi-Section Invoice PDF

```csharp
public async Task<ReportResponse> BuildInvoicePdfAsync(
    InvoiceData invoice,
    CancellationToken ct = default)
{
    var request = new QuestPdfReportRequest
    {
        Title           = $"Invoice #{invoice.Number}",
        SubTitle        = $"Issued: {invoice.Date:yyyy-MM-dd}",
        Author          = "Acontplus ERP",
        FileDownloadName = $"Invoice_{invoice.Number}",

        Settings = new QuestPdfDocumentSettings
        {
            PageSize    = QuestPdfPageSize.A4,
            Orientation = QuestPdfPageOrientation.Portrait,
            FontFamily  = "Helvetica",
            FontSize    = 9f,
            ShowPageNumbers = true,
            ShowTimestamp   = false,

            ColorTheme = QuestPdfColorThemes.AcontplusDefault()
        },

        GlobalHeader = new QuestPdfHeaderFooterOptions
        {
            LogoPath        = "wwwroot/images/logo.png",
            RightText       = "Acontplus ERP",
            BackgroundColor = "#d61672",
            ShowBorderBottom = false
        },

        Sections =
        [
            // ── 1. Key-value summary ──────────────────────────────────────
            new QuestPdfSection
            {
                SectionTitle = "Invoice Details",
                Type         = QuestPdfSectionType.KeyValueSummary,
                KeyValues    = new()
                {
                    ["Invoice No."]    = invoice.Number,
                    ["Date"]           = invoice.Date.ToString("yyyy-MM-dd"),
                    ["Customer"]       = invoice.CustomerName,
                    ["Address"]        = invoice.CustomerAddress,
                    ["Payment Terms"]  = invoice.PaymentTerms
                }
            },

            // ── 2. Line items table ───────────────────────────────────────
            new QuestPdfSection
            {
                SectionTitle = "Line Items",
                Type         = QuestPdfSectionType.DataTable,
                Data         = invoice.LineItemsTable,   // DataTable
                ShowTotalsRow = true,
                Columns      =
                [
                    new QuestPdfTableColumn { ColumnName = "LineNo",      Header = "#",         RelativeWidth = 0.5f },
                    new QuestPdfTableColumn { ColumnName = "Description", Header = "Description", RelativeWidth = 4f   },
                    new QuestPdfTableColumn { ColumnName = "Qty",         Header = "Qty",        RelativeWidth = 1f, Alignment = QuestPdfColumnAlignment.Right, Format = "N2" },
                    new QuestPdfTableColumn { ColumnName = "UnitPrice",   Header = "Unit Price", RelativeWidth = 1.5f, Alignment = QuestPdfColumnAlignment.Right, Format = "C2" },
                    new QuestPdfTableColumn { ColumnName = "Total",       Header = "Total",      RelativeWidth = 1.5f, Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                             AggregateType = QuestPdfAggregateType.Sum, IsBold = true }
                ]
            },

            // ── 3. Totals text block ──────────────────────────────────────
            new QuestPdfSection
            {
                Type       = QuestPdfSectionType.Text,
                TextBlocks =
                [
                    new QuestPdfTextBlock { Content = $"Subtotal:  {invoice.Subtotal:C2}", Bold = true },
                    new QuestPdfTextBlock { Content = $"Tax (12%): {invoice.Tax:C2}"                  },
                    new QuestPdfTextBlock { Content = $"TOTAL DUE: {invoice.Total:C2}", Bold = true, FontSize = 12f, Color = "#1E3A5F" }
                ]
            },

            // ── 4. Custom section via delegate ────────────────────────────
            new QuestPdfSection
            {
                SectionTitle = "Payment Instructions",
                Type         = QuestPdfSectionType.Custom,
                CustomComposer = container =>
                {
                    container
                        .Background("#F0F4FF")
                        .Padding(10)
                        .Column(col =>
                        {
                            col.Item().Text("Bank Transfer").Bold().FontSize(10);
                            col.Item().Text("Account: 1234-5678-9012").FontSize(9);
                            col.Item().Text("SWIFT: ACONTPLUSXXX").FontSize(9);
                        });
                }
            }
        ]
    };

    return await _pdf.GenerateAsync(request, ct);
}
```

---

### Thermal / Receipt-Width Report

```csharp
var request = new QuestPdfReportRequest
{
    Title    = "Receipt",
    Settings = new QuestPdfDocumentSettings
    {
        PageSize    = QuestPdfPageSize.Thermal80mm,   // 80mm wide
        MarginLeft  = 5f,
        MarginRight = 5f,
        FontSize    = 8f
    },
    Sections =
    [
        new QuestPdfSection
        {
            Type = QuestPdfSectionType.KeyValueSummary,
            KeyValues = new() { ["Store"] = "Acontplus POS", ["Cashier"] = "Jane" }
        },
        new QuestPdfSection
        {
            Type = QuestPdfSectionType.DataTable,
            Data = receiptItemsTable,
            Columns =
            [
                new QuestPdfTableColumn { ColumnName = "Item",  RelativeWidth = 3f },
                new QuestPdfTableColumn { ColumnName = "Total", RelativeWidth = 1f, Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                         AggregateType = QuestPdfAggregateType.Sum }
            ]
        }
    ]
};
var receipt = await _pdf.GenerateAsync(request, ct);
```

---

### QuestPDF DTOs Reference

#### `QuestPdfReportRequest`

| Property           | Type                           | Description                           |
| ------------------ | ------------------------------ | ------------------------------------- |
| `Title`            | `string`                       | Document title (also in PDF metadata) |
| `SubTitle`         | `string?`                      | Optional subtitle below title         |
| `Author`           | `string?`                      | PDF metadata author                   |
| `FileDownloadName` | `string?`                      | Suggested download filename           |
| `Settings`         | `QuestPdfDocumentSettings`     | Page & theme config                   |
| `GlobalHeader`     | `QuestPdfHeaderFooterOptions?` | Per-page header                       |
| `GlobalFooter`     | `QuestPdfHeaderFooterOptions?` | Per-page footer                       |
| `Sections`         | `List<QuestPdfSection>`        | Ordered content blocks                |

#### `QuestPdfDocumentSettings`

| Property          | Default                                  | Description                                           |
| ----------------- | ---------------------------------------- | ----------------------------------------------------- |
| `PageSize`        | `A4`                                     | Page size preset                                      |
| `Orientation`     | `Portrait`                               | Portrait or Landscape                                 |
| `FontFamily`      | `"Helvetica"`                            | Default font family                                   |
| `FontSize`        | `9f`                                     | Default body font size (pt)                           |
| `ShowPageNumbers` | `true`                                   | Auto page number footer                               |
| `ShowTimestamp`   | `false`                                  | Show UTC timestamp in footer                          |
| `LicenseType`     | `Community`                              | QuestPDF license tier                                 |
| `ColorTheme`      | `QuestPdfColorThemes.AcontplusDefault()` | Full visual theme — see [Color Themes](#color-themes) |

#### Color Themes

Use the `QuestPdfColorThemes` static factory for ready-made presets that match the **Acontplus brand palette** (sourced from the AcontplusWeb design system).

| Preset               | Description                                                                        |
| -------------------- | ---------------------------------------------------------------------------------- |
| `AcontplusDefault()` | Magenta header `#d61672`, blush alternates `#fdf2f8`, wine totals — official brand |
| `AcontplusAmber()`   | Amber/gold header `#ffa901`, warm alternates — seasonal or financial reports       |
| `Corporate()`        | Navy header `#1E3A5F`, slate alternates — conservative enterprise documents        |
| `Ocean()`            | Teal/cyan header `#0077B6`, cyan alternates — tech products and dashboards         |
| `Monochrome()`       | Slate-grey header `#334155`, zero colour — legal or print-monochrome reports       |

```csharp
// Quick preset
ColorTheme = QuestPdfColorThemes.AcontplusDefault()

// Preset with individual override
var theme = QuestPdfColorThemes.Corporate();
theme.BorderColor = "#BDC3C7";
ColorTheme = theme

// Fully manual
ColorTheme = new QuestPdfColorTheme
{
    HeaderBackground     = "#d61672",   // brand primary
    AlternateRowBackground = "#fdf2f8", // brand light
    AccentColor          = "#d61672",
    TotalsBackground     = "#fce7f3",
    TotalsTextColor      = "#831843",
    KvKeyColor           = "#be185d",
    FooterTextColor      = "#8c8c8c",
    BorderColor          = "#eaeaea",
    SuccessColor         = "#10b981",
    WarningColor         = "#f59e0b",
    ErrorColor           = "#ef4444"
}
```

##### `QuestPdfColorTheme` properties

| Property                 | Default   | Description                            |
| ------------------------ | --------- | -------------------------------------- |
| `HeaderBackground`       | `#d61672` | Table header / page-header background  |
| `HeaderForeground`       | `#FFFFFF` | Table header text color                |
| `RowBackground`          | `#FFFFFF` | Even row background                    |
| `AlternateRowBackground` | `#fdf2f8` | Odd (alternate) row background         |
| `TotalsBackground`       | `#fce7f3` | Totals row background                  |
| `TotalsTextColor`        | `#831843` | Totals row text color                  |
| `AccentColor`            | `#d61672` | Section titles and dividers            |
| `SecondaryAccentColor`   | `#ffa901` | Charts, badges, call-out cells         |
| `TextColor`              | `#252525` | Default body text                      |
| `MutedTextColor`         | `#8c8c8c` | Dates, subtitles, empty-state messages |
| `KvKeyColor`             | `#be185d` | Key-value label (left column) color    |
| `FooterTextColor`        | `#8c8c8c` | Page footer text and page numbers      |
| `BorderColor`            | `#eaeaea` | Row borders and section separators     |
| `SuccessColor`           | `#10b981` | Success status indicators              |
| `WarningColor`           | `#f59e0b` | Warning status indicators              |
| `ErrorColor`             | `#ef4444` | Error / danger indicators              |

#### `QuestPdfTableColumn`

| Property        | Default  | Description                                        |
| --------------- | -------- | -------------------------------------------------- |
| `ColumnName`    | required | DataTable column name                              |
| `Header`        | `null`   | Display label (uses ColumnName if null)            |
| `RelativeWidth` | `null`   | Proportional width (auto-split if null)            |
| `Alignment`     | `Left`   | Cell text alignment                                |
| `Format`        | `null`   | .NET format string: `"C2"`, `"N0"`, `"yyyy-MM-dd"` |
| `AggregateType` | `None`   | Totals row: `Sum`, `Count`, `Average`              |
| `IsBold`        | `false`  | Bold cell text                                     |
| `IsHidden`      | `false`  | Exclude from output                                |

#### `QuestPdfSection` types

| `Type`            | Required properties | Description                                    |
| ----------------- | ------------------- | ---------------------------------------------- |
| `DataTable`       | `Data`              | Renders a `DataTable` as a themed grid         |
| `Text`            | `TextBlocks`        | Renders a list of formatted text blocks        |
| `KeyValueSummary` | `KeyValues`         | Renders a two-column label/value panel         |
| `Custom`          | `CustomComposer`    | Full control via `Action<IContainer>` delegate |

#### Page Sizes

`A4` · `A3` · `A5` · `Letter` · `Legal` · `Tabloid` · `Executive` · `Thermal80mm`

---

### Configuration Options (QuestPDF additions)

| Option                         | Type                  | Default                         | Description                |
| ------------------------------ | --------------------- | ------------------------------- | -------------------------- |
| `QuestPdfLicenseType`          | `QuestPdfLicenseType` | `Community`                     | QuestPDF license tier      |
| `MaxConcurrentQuestPdfReports` | `int?`                | inherits `MaxConcurrentReports` | Dedicated concurrency slot |

---

## 📚 Advanced Configuration

### Configuration Options Reference

| Option                           | Type      | Default             | Description                                            |
| -------------------------------- | --------- | ------------------- | ------------------------------------------------------ |
| `MainDirectory`                  | `string`  | `"Reports"`         | Main directory for RDLC files (relative to app base)   |
| `ExternalDirectory`              | `string?` | `null`              | External directory for offline reports (absolute path) |
| `MaxReportSizeBytes`             | `long`    | `104857600` (100MB) | Maximum output size in bytes                           |
| `ReportGenerationTimeoutSeconds` | `int`     | `300` (5min)        | Timeout for report generation                          |
| `PrintJobTimeoutSeconds`         | `int`     | `180` (3min)        | Timeout for print jobs                                 |
| `EnableReportDefinitionCache`    | `bool`    | `true`              | Enable template caching                                |
| `MaxCachedReportDefinitions`     | `int`     | `100`               | Maximum cached templates                               |
| `CacheTtlMinutes`                | `int`     | `60`                | Cache expiration time                                  |
| `EnableMemoryPooling`            | `bool`    | `true`              | Enable memory pooling                                  |
| `MaxConcurrentReports`           | `int`     | `10`                | Max concurrent report generations                      |
| `MaxConcurrentPrintJobs`         | `int`     | `5`                 | Max concurrent print jobs                              |
| `EnableDetailedLogging`          | `bool`    | `false`             | Detailed performance logging                           |

### DTOs Reference

#### ReportPropsDto

Defines report properties (required in `parameters` DataSet as "ReportProps" table):

```csharp
public class ReportPropsDto
{
    public required string ReportPath { get; set; }      // e.g., "InvoiceReport.rdlc"
    public required string ReportName { get; set; }      // e.g., "Invoice_123"
    public required string ReportFormat { get; set; }    // "PDF", "EXCEL", "EXCELOPENXML", etc.
}
```

#### RdlcPrinterDto

Configures printer settings:

```csharp
public class RdlcPrinterDto
{
    public required string PrinterName { get; set; }      // Windows printer name
    public required string FileName { get; set; }         // RDLC file name
    public required string Format { get; set; }           // "IMAGE" for thermal printers
    public required string ReportsDirectory { get; set; } // Path to Reports folder
    public required string LogoDirectory { get; set; }    // Path to Images folder
    public required string LogoName { get; set; }         // Logo file name (no extension)
    public required string DeviceInfo { get; set; }       // XML device configuration
    public short Copies { get; set; }                     // Number of copies
}
```

#### RdlcPrintRequestDto

Contains print data:

```csharp
public class RdlcPrintRequestDto
{
    // Data sources: DataSet name -> List of rows (as dictionaries)
    public required Dictionary<string, List<Dictionary<string, string>>> DataSources { get; set; }

    // Report parameters: parameter name -> value
    public required Dictionary<string, string> ReportParams { get; set; }
}
```

#### ReportResponse

Report output (implements IDisposable):

```csharp
public class ReportResponse : IDisposable
{
    public byte[] FileContents { get; set; }      // Report binary data
    public string ContentType { get; set; }       // MIME type
    public string FileDownloadName { get; set; }  // Suggested filename
}
```

### Exception Handling

All exceptions inherit from `ReportGenerationException`:

```csharp
// Base exception
catch (ReportGenerationException ex)
{
    _logger.LogError(ex, "Report error: {Message}", ex.Message);
    // Properties: ReportPath, ReportFormat
}

// Specific exceptions
catch (ReportNotFoundException ex)
{
    // Report template (.rdlc) file not found
    return NotFound($"Template not found: {ex.ReportPath}");
}

catch (ReportTimeoutException ex)
{
    // Generation or print exceeded timeout
    return StatusCode(504, $"Timeout after {ex.TimeoutSeconds}s");
}

catch (ReportSizeExceededException ex)
{
    // Output exceeded MaxReportSizeBytes
    return StatusCode(413, $"Size {ex.ReportSize} exceeds max {ex.MaxSize}");
}
```

### Supported Export Formats

| Format         | Enum Value     | Content Type                                                              | Extension |
| -------------- | -------------- | ------------------------------------------------------------------------- | --------- |
| PDF            | `PDF`          | `application/pdf`                                                         | `.pdf`    |
| Excel (Legacy) | `EXCEL`        | `application/vnd.ms-excel`                                                | `.xls`    |
| Excel (Modern) | `EXCELOPENXML` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`       | `.xlsx`   |
| Word           | `WORDOPENXML`  | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `.docx`   |
| HTML           | `HTML5`        | `text/html`                                                               | `.html`   |
| Image          | `IMAGE`        | `image/jpeg`                                                              | `.jpg`    |

### Performance Tuning

#### High Concurrency Scenario

```csharp
// E-commerce, multi-tenant, high traffic
options.MaxConcurrentReports = 50;
options.MaxConcurrentPrintJobs = 20;
options.EnableReportDefinitionCache = true;
options.MaxCachedReportDefinitions = 200;
options.ReportGenerationTimeoutSeconds = 120;
options.EnableMemoryPooling = true;
```

#### Large Reports Scenario

```csharp
// Financial statements, data warehouses, complex reports
options.MaxReportSizeBytes = 500 * 1024 * 1024; // 500 MB
options.ReportGenerationTimeoutSeconds = 600; // 10 minutes
options.MaxConcurrentReports = 5; // Limit concurrent large reports
options.EnableMemoryPooling = true;
```

#### Memory-Constrained Scenario

```csharp
// Cloud containers, shared hosting, limited resources
options.MaxConcurrentReports = 3;
options.MaxConcurrentPrintJobs = 2;
options.MaxCachedReportDefinitions = 20;
options.CacheTtlMinutes = 15;
options.MaxReportSizeBytes = 10 * 1024 * 1024; // 10 MB
```

#### POS/Retail Scenario

```csharp
// Point of sale, thermal printers, receipts
options.MaxConcurrentPrintJobs = 10; // Multiple registers
options.PrintJobTimeoutSeconds = 60; // Fast printing
options.EnableReportDefinitionCache = true;
options.MaxCachedReportDefinitions = 50; // Receipt templates
options.EnableDetailedLogging = true; // Track print failures
```

## 🔍 Best Practices

### 1. Use Async Methods

```csharp
// ✅ Good - Async
var report = await _reportService.GetReportAsync(params, data, false, ct);

// ❌ Avoid - Synchronous (deprecated)
var report = _reportService.GetReport(params, data, false);
```

### 2. Implement Proper Cancellation

```csharp
[HttpGet("report")]
public async Task<IActionResult> GetReport(CancellationToken cancellationToken)
{
    // Cancellation token is automatically passed from HTTP request
    var report = await _reportService.GetReportAsync(
        params, data, false, cancellationToken);

    return File(report.FileContents, report.ContentType, report.FileDownloadName);
}
```

### 3. Dispose Report Responses

```csharp
// ✅ Using statement ensures disposal
using var report = await _reportService.GetReportAsync(params, data);
ProcessReport(report);

// Or explicitly
var report = await _reportService.GetReportAsync(params, data);
try
{
    ProcessReport(report);
}
finally
{
    report.Dispose();
}
```

### 4. Structure Your DataSets Correctly

```csharp
// Parameters DataSet structure:
// - ReportProps table (required): ReportPath, ReportName, ReportFormat
// - ReportParams table (optional): paramName, paramValue, isPicture, isCompressed

// Data DataSet structure:
// - One or more tables matching your RDLC DataSet names
// - Column names must match RDLC field names exactly
```

### 5. Handle Exceptions Gracefully

```csharp
try
{
    var report = await _reportService.GetReportAsync(params, data, false, ct);
    return File(report.FileContents, report.ContentType, report.FileDownloadName);
}
catch (ReportNotFoundException ex)
{
    return NotFound(new { error = "Template not found", details = ex.Message });
}
catch (ReportTimeoutException ex)
{
    return StatusCode(504, new { error = "Timeout", seconds = ex.TimeoutSeconds });
}
catch (ReportSizeExceededException ex)
{
    return StatusCode(413, new { error = "Too large", size = ex.ReportSize });
}
catch (ReportGenerationException ex)
{
    _logger.LogError(ex, "Report failed");
    return StatusCode(500, new { error = "Generation failed" });
}
```

### 6. Enable Logging for Troubleshooting

```csharp
// Development
options.EnableDetailedLogging = true;

// Production (only enable when needed)
options.EnableDetailedLogging = false;
```

## 🧪 Testing

### Unit Test Example

```csharp
public class ReportServiceTests
{
    private readonly Mock<ILogger<RdlcReportService>> _loggerMock;
    private readonly Mock<IOptions<ReportOptions>> _optionsMock;
    private readonly RdlcReportService _service;

    public ReportServiceTests()
    {
        _loggerMock = new Mock<ILogger<RdlcReportService>>();
        _optionsMock = new Mock<IOptions<ReportOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new ReportOptions());
        _service = new RdlcReportService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task GenerateReport_ValidData_ReturnsReport()
    {
        // Arrange
        var parameters = CreateTestParameters("TestReport.rdlc", "Test", "PDF");
        var data = CreateTestData();

        // Act
        var result = await _service.GetReportAsync(parameters, data);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FileContents);
        Assert.True(result.FileContents.Length > 0);
        Assert.Equal("application/pdf", result.ContentType);
    }
}
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](../../CONTRIBUTING.md) for details.

### Development Setup

```bash
git clone https://github.com/acontplus/acontplus-dotnet-libs.git
cd acontplus-dotnet-libs
dotnet restore
dotnet build
dotnet test
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

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
