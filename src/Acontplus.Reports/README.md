# Acontplus.Reports

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Reports.svg)](https://www.nuget.org/packages/Acontplus.Reports)

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance .NET library for enterprise report generation. Covers RDLC reports, QuestPDF code-first PDF documents, **MiniExcel** streaming Excel exports, and **ClosedXML** richly-formatted Excel workbooks — all with async APIs, concurrency control, timeout protection, and full DI integration.

## 🚀 Features

### Core Capabilities

- ✅ **Async/Await Support** - Fully asynchronous API for better scalability
- ✅ **RDLC Report Generation** - Support for PDF, Excel, Word, HTML5, Image exports
- ✅ **QuestPDF Dynamic PDF** - Fluent code-first PDF generation without design-time files
- ✅ **MiniExcel Streaming Excel** - Ultra-fast, low-memory bulk data exports (DataTable / POCO)
- ✅ **ClosedXML Advanced Excel** - Richly formatted workbooks: corporate styles, freeze panes, AutoFilter, formula aggregates
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

---

## 🖥️ Platform Compatibility

Different services in this library have different platform requirements. Always check this table before choosing a service in a cross-platform deployment (Linux containers, cloud, macOS CI).

| Service         | Interface                 | Windows | Linux | macOS | Notes                                                                                                                                                                |
| --------------- | ------------------------- | :-----: | :---: | :---: | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RDLC generation | `IRdlcReportService`      |   ✅    |  ❌   |  ❌   | **Windows only.** Depends on `ReportViewerCore.NETCore` which requires GDI+ / `System.Drawing.Common`. Does not work on Linux or macOS regardless of `libgdiplus`.   |
| RDLC printing   | `IRdlcPrinterService`     |   ✅    |  ❌   |  ❌   | **Windows 6.1+ only.** Registered automatically when `OperatingSystem.IsWindowsVersionAtLeast(6,1)` returns `true`; injecting it on other OSes will fail at runtime. |
| QuestPDF PDF    | `IQuestPdfReportService`  |   ✅    |  ✅   |  ✅   | Fully cross-platform. No native dependencies. Preferred choice for PDF in containerized / cloud environments. Requires a valid QuestPDF license (Community is free). |
| MiniExcel       | `IMiniExcelReportService` |   ✅    |  ✅   |  ✅   | Fully cross-platform. Pure managed code; no GDI+ or COM dependencies.                                                                                                |
| ClosedXML       | `IClosedXmlReportService` |   ✅    |  ✅   |  ✅   | Fully cross-platform. Pure managed code; no GDI+ or COM dependencies.                                                                                                |

### Linux / Docker / macOS

> **Both RDLC services are Windows-only.** `IRdlcReportService` depends on `ReportViewerCore.NETCore` which does not run on Linux or macOS — `libgdiplus` is **not** sufficient to make it work. `IRdlcPrinterService` additionally requires Windows 6.1+ at the OS level.
>
> **Recommendation for containers and cross-platform deployments:** use `IQuestPdfReportService` for PDF, `IMiniExcelReportService` / `IClosedXmlReportService` for Excel. Never reference either RDLC service on non-Windows hosts.

---

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
  <PackageReference Include="Acontplus.Reports" Version="1.7.0" />
</ItemGroup>
```

---

## 🔀 Service Selection Guide

Use this table to pick the right service for each use-case before writing any code.

| Requirement                        | Recommended service                          | Reason                                                                        |
| ---------------------------------- | -------------------------------------------- | ----------------------------------------------------------------------------- |
| PDF from existing `.rdlc` template | `IRdlcReportService`                         | Template-driven; **Windows only** — does not run on Linux or macOS            |
| PDF via code (no template file)    | `IQuestPdfReportService`                     | Code-first, cross-platform, fluent composition                                |
| PDF in Linux / Docker              | `IQuestPdfReportService`                     | **Only cross-platform PDF engine** — no GDI+ required                         |
| PDF for thermal/receipt printer    | `IRdlcPrinterService` + `IRdlcReportService` | Direct Windows printing; Windows 6.1+ only                                    |
| Bulk data download (CSV/Excel)     | `IMiniExcelReportService`                    | Streaming — constant memory regardless of row count; fastest for large tables |
| Large DataTable API export         | `IMiniExcelReportService`                    | Minimal allocations; no in-memory DOM                                         |
| POCO collection export             | `IMiniExcelReportService`                    | Native generic serialisation via `GenerateFromObjectsAsync<T>`                |
| Formatted report for end users     | `IClosedXmlReportService`                    | Corporate styles, freeze panes, AutoFilter, formula totals                    |
| Invoice / statement / dashboard    | `IClosedXmlReportService`                    | Rich formatting; users can open and edit the resulting workbook               |
| Multi-sheet analytical workbook    | Both work; prefer `IClosedXmlReportService`  | ClosedXML adds per-sheet styles and formula rows; MiniExcel for raw data only |
| Cross-platform Excel in containers | Either Excel service                         | Both are fully cross-platform; no native dependencies                         |

### Quick decision diagram

```
Do you need PDF?
├─ Yes — Do you have an .rdlc template?
│   ├─ Yes, Windows only → IRdlcReportService
│   └─ No, or cross-platform → IQuestPdfReportService  ← preferred
└─ No — Do you need Excel?
    ├─ Bulk / large data / POCO collection → IMiniExcelReportService
    └─ Formatted report / corporate styles / formulas → IClosedXmlReportService
```

---

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

> **Platform: Windows only ✅ / Linux ❌ / macOS ❌ / Docker ❌.**
> `IRdlcReportService` depends on `ReportViewerCore.NETCore` (GDI+) and does not run on Linux or macOS.
> For cross-platform PDF generation use `IQuestPdfReportService` instead.

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

> **Platform: Windows 6.1+ only.** `IRdlcPrinterService` is only registered when `OperatingSystem.IsWindowsVersionAtLeast(6, 1)` returns `true`. Do **not** inject or resolve this service on Linux or macOS — it will not be available in the DI container.

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

> **Platform:** Windows ✅ · Linux ✅ · macOS ✅ · Docker ✅ — **fully cross-platform, no native dependencies.**

`IQuestPdfReportService` provides code-first, fluent PDF generation via **QuestPDF 2026.x** — no RDLC templates, no design-time files. Reports are programmatically composed from typed request objects and rendered on any platform (.NET 10 / Linux / Windows / macOS). This is the **recommended PDF engine** for containerized and cloud-native workloads.

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

| Property            | Default                                  | Description                                           |
| ------------------- | ---------------------------------------- | ----------------------------------------------------- |
| `PageSize`          | `A4`                                     | Page size preset                                      |
| `Orientation`       | `Portrait`                               | Portrait or Landscape                                 |
| `FontFamily`        | `"Helvetica"`                            | Default font family                                   |
| `FontSize`          | `9f`                                     | Default body font size (pt)                           |
| `ShowPageNumbers`   | `true`                                   | Auto page number footer                               |
| `ShowTimestamp`     | `false`                                  | Show UTC timestamp in footer                          |
| `ShowWatermark`     | `false`                                  | Enable diagonal watermark overlay                     |
| `WatermarkText`     | `null`                                   | Watermark text (requires `ShowWatermark = true`)      |
| `WatermarkFontSize` | `80f`                                    | **New v1.8.0.** Watermark font size in points         |
| `WatermarkColor`    | `"#EEEEEE"`                              | **New v1.8.0.** Watermark text colour (HTML hex)      |
| `LicenseType`       | `Community`                              | QuestPDF license tier                                 |
| `ColorTheme`        | `QuestPdfColorThemes.AcontplusDefault()` | Full visual theme — see [Color Themes](#color-themes) |

#### `QuestPdfHeaderFooterOptions` — new v1.8.0 logo properties

| Property       | Default | Description                                                                                                                                        |
| -------------- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `LogoBytes`    | `null`  | **New v1.8.0.** Logo image bytes — bypasses the file system entirely. Takes priority over `LogoPath`. Ideal when the logo is stored in a database. |
| `LogoMimeType` | `null`  | MIME type hint for `LogoBytes` (e.g. `"image/png"`).                                                                                               |

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

| Property        | Default  | Description                                                                                                 |
| --------------- | -------- | ----------------------------------------------------------------------------------------------------------- |
| `ColumnName`    | required | DataTable column name (ignored when `IsGroupHeader = true`)                                                 |
| `Header`        | `null`   | Display label (uses ColumnName if null)                                                                     |
| `RelativeWidth` | `null`   | Proportional width (auto-split if null)                                                                     |
| `Alignment`     | `Left`   | Cell text alignment                                                                                         |
| `Format`        | `null`   | .NET format string: `"C2"`, `"N0"`, `"yyyy-MM-dd"`                                                          |
| `AggregateType` | `None`   | Totals row: `Sum`, `Count`, `Average`                                                                       |
| `IsBold`        | `false`  | Bold cell text                                                                                              |
| `IsHidden`      | `false`  | Exclude from output                                                                                         |
| `IsGroupHeader` | `false`  | **New v1.8.0.** Renders as a band/group header row spanning `ColumnSpan` columns. No `ColumnName` required. |
| `ColumnSpan`    | `1`      | **New v1.8.0.** Number of data columns this band header spans (used only when `IsGroupHeader = true`).      |

> **Grouped-header layout (Kardex-style):** Mix normal `QuestPdfTableColumn` entries with group-header descriptors (`IsGroupHeader = true, ColumnSpan = N`). Group descriptors appear as a coloured band row _above_ the normal header row, spanning the stated number of data columns left-to-right in the order they are declared.

#### `QuestPdfSection` types

| `Type`            | Required properties                                        | Description                                                                                                                                 |
| ----------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `DataTable`       | `Data`                                                     | Renders a `DataTable` as a themed grid                                                                                                      |
| `Text`            | `TextBlocks`                                               | Renders a list of formatted text blocks                                                                                                     |
| `KeyValueSummary` | `KeyValues`                                                | Renders a two-column label/value panel                                                                                                      |
| `Custom`          | `CustomComposer`                                           | Full control via `Action<IContainer>` delegate                                                                                              |
| `Image`           | `ImageBytes`                                               | **New v1.8.0.** Renders a raw image (`byte[]`) with optional max width/height and alignment.                                                |
| `Barcode`         | `BarcodeText` or `BarcodeBytes`                            | **New v1.8.0.** Generates a Code-128 or QR code image via `Acontplus.Barcode`.                                                              |
| `MasterDetail`    | `Data`, `DetailData`, `MasterKeyColumn`, `DetailKeyColumn` | **New v1.8.0.** Master rows each followed by a filtered detail sub-table (EstadoCuenta / Statement pattern).                                |
| `TwoColumn`       | `LeftContentType`, `RightSection`                          | **New v1.8.0.** Side-by-side columns; left renders the parent section using `LeftContentType`, right renders an independent `RightSection`. |
| `InvoiceHeader`   | `InvoiceHeader`                                            | **New v1.8.0.** SRI Ecuador–style invoice header: company block (left) + SRI auth box (right) + buyer band (bottom).                        |

##### `QuestPdfSection` — new v1.8.0 property groups

**Image properties** (`Type = Image`)

| Property         | Default | Description                              |
| ---------------- | ------- | ---------------------------------------- |
| `ImageBytes`     | `null`  | Raw image bytes                          |
| `ImageMaxHeight` | `80f`   | Maximum height in points                 |
| `ImageMaxWidth`  | `160f`  | Maximum width in points (`0` = no limit) |
| `ImageAlignment` | `Left`  | Cell alignment                           |

**Barcode properties** (`Type = Barcode`)

| Property             | Default   | Description                                                    |
| -------------------- | --------- | -------------------------------------------------------------- |
| `BarcodeText`        | `null`    | Source text — barcode is generated at render time              |
| `BarcodeBytes`       | `null`    | Pre-rendered barcode image (takes priority over `BarcodeText`) |
| `BarcodeType`        | `Code128` | `QuestPdfBarcodeType.Code128` or `QrCode`                      |
| `BarcodeWidth`       | `120f`    | Rendered width in points                                       |
| `BarcodeHeight`      | `50f`     | Rendered height in points                                      |
| `BarcodeAlignment`   | `Center`  | Alignment inside the container                                 |
| `ShowBarcodeCaption` | `false`   | Include human-readable text label below the barcode            |

**MasterDetail properties** (`Type = MasterDetail`)

| Property              | Default | Description                                   |
| --------------------- | ------- | --------------------------------------------- |
| `MasterKeyColumn`     | `null`  | Column in `Data` used as join key             |
| `DetailKeyColumn`     | `null`  | Column in `DetailData` used as join key       |
| `DetailData`          | `null`  | DataTable containing the detail rows          |
| `DetailColumns`       | `null`  | Column definitions for the detail sub-table   |
| `DetailSectionTitle`  | `null`  | Optional heading above each detail sub-table  |
| `ShowDetailTotalsRow` | `false` | Show aggregate totals on the detail sub-table |

**TwoColumn properties** (`Type = TwoColumn`)

| Property           | Default     | Description                                                                                     |
| ------------------ | ----------- | ----------------------------------------------------------------------------------------------- |
| `LeftContentType`  | `DataTable` | Section type to render in the left column (uses the parent section's data/keyvalues/textblocks) |
| `RightSection`     | `null`      | Fully independent `QuestPdfSection` rendered in the right column                                |
| `LeftColumnRatio`  | `1`         | Proportional width of the left column                                                           |
| `RightColumnRatio` | `1`         | Proportional width of the right column                                                          |
| `TwoColumnGap`     | `8f`        | Gap in points between the two columns                                                           |

#### `QuestPdfInvoiceHeader` (new v1.8.0)

Used by `Type = InvoiceHeader` — models the standard SRI Ecuador electronic invoice header layout.

| Property                | Default     | Description                                                          |
| ----------------------- | ----------- | -------------------------------------------------------------------- |
| `LogoBytes`             | `null`      | Company logo from database / memory (takes priority over `LogoPath`) |
| `LogoPath`              | `null`      | Company logo from file system                                        |
| `LogoMaxHeight`         | `50f`       | Maximum logo height in points                                        |
| `CompanyName`           | `null`      | Legal company name                                                   |
| `TradeName`             | `null`      | Commercial name                                                      |
| `CompanyAddress`        | `null`      | Registered address                                                   |
| `BranchAddress`         | `null`      | Branch / establishment address                                       |
| `CompanyPhone`          | `null`      | Contact phone                                                        |
| `CompanyEmail`          | `null`      | Contact e-mail                                                       |
| `CompanyActivity`       | `null`      | Commercial activity                                                  |
| `ContribuyenteEspecial` | `null`      | SRI special contributor number                                       |
| `ObligadoContabilidad`  | `null`      | `"SI"` / `"NO"`                                                      |
| `ContribuyenteRimpe`    | `null`      | RIMPE regime label                                                   |
| `AgenteRetencion`       | `null`      | Retention agent resolution                                           |
| `Ruc`                   | `null`      | RUC (tax ID)                                                         |
| `DocumentType`          | `null`      | Document type label e.g. `"FACTURA"`                                 |
| `DocumentNumber`        | `null`      | Sequential number `001-001-000000042`                                |
| `AuthorizationNumber`   | `null`      | SRI authorization number                                             |
| `AuthorizationDate`     | `null`      | Authorization date/time string                                       |
| `AccessKey`             | `null`      | 49-character SRI access key                                          |
| `Environment`           | `null`      | `"PRODUCCIÓN"` / `"PRUEBAS"`                                         |
| `EmissionType`          | `null`      | `"EMISIÓN NORMAL"`                                                   |
| `BuyerName`             | `null`      | Buyer legal name                                                     |
| `BuyerIdentification`   | `null`      | Buyer RUC / CI                                                       |
| `BuyerAddress`          | `null`      | Buyer address                                                        |
| `EmissionDate`          | `null`      | Emission date string                                                 |
| `DeliveryReference`     | `null`      | Dispatch / remission guide number                                    |
| `ExtraFields`           | `{}`        | Additional buyer-block key-value pairs                               |
| `LeftPanelRatio`        | `6`         | Proportional width of company block                                  |
| `RightPanelRatio`       | `4`         | Proportional width of SRI auth box                                   |
| `AuthBoxBorderColor`    | `"#d61672"` | Border colour of the SRI auth box                                    |
| `FontSize`              | `8f`        | Base font size in points                                             |

#### Page Sizes

`A4` · `A3` · `A5` · `Letter` · `Legal` · `Tabloid` · `Executive` · `Thermal80mm`

---

### Configuration Options (QuestPDF additions)

| Option                         | Type                  | Default                         | Description                |
| ------------------------------ | --------------------- | ------------------------------- | -------------------------- |
| `QuestPdfLicenseType`          | `QuestPdfLicenseType` | `Community`                     | QuestPDF license tier      |
| `MaxConcurrentQuestPdfReports` | `int?`                | inherits `MaxConcurrentReports` | Dedicated concurrency slot |

---

## � Excel Report Generation

The library ships two complementary Excel engines. Both are **fully cross-platform** (Windows ✅ · Linux ✅ · macOS ✅ · Docker ✅) with no GDI+ or COM dependencies. Choose based on your requirements:

| Feature            | `IMiniExcelReportService`                               | `IClosedXmlReportService`                           |
| ------------------ | ------------------------------------------------------- | --------------------------------------------------- |
| Engine             | [MiniExcel](https://github.com/mini-software/MiniExcel) | [ClosedXML](https://github.com/ClosedXML/ClosedXML) |
| Platform           | ✅ Cross-platform                                       | ✅ Cross-platform                                   |
| Memory model       | Streaming (no DOM)                                      | In-memory workbook                                  |
| Best for           | Bulk data, large tables, APIs                           | Reports, invoices, dashboards                       |
| Corporate styles   | ❌                                                      | ✅                                                  |
| Freeze panes       | ❌                                                      | ✅                                                  |
| AutoFilter         | ❌                                                      | ✅                                                  |
| Aggregate formulas | ❌                                                      | ✅                                                  |
| Alternating rows   | ❌                                                      | ✅                                                  |
| POCO collections   | ✅                                                      | ❌                                                  |
| Multi-sheet        | ✅                                                      | ✅                                                  |

> Both services share the same `AddReportServices()` registration and `ReportOptions` timeout/concurrency settings.

---

### Registration (standalone)

```csharp
// MiniExcel only
builder.Services.AddMiniExcelReportService(builder.Configuration);

// ClosedXML only
builder.Services.AddClosedXmlReportService(builder.Configuration);

// Or both + full stack (RDLC + QuestPDF + MiniExcel + ClosedXML)
builder.Services.AddReportServices(builder.Configuration);
```

---

### MiniExcel — Quick Start

Best for bulk data exports where speed and low memory consumption matter.

```csharp
using Acontplus.Reports.Interfaces;
using Acontplus.Reports.Dtos;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IMiniExcelReportService _excel;

    public ExportController(IMiniExcelReportService excel) => _excel = excel;

    // Quick single-table export
    [HttpGet("simple")]
    public async Task<IActionResult> ExportSimple(CancellationToken ct)
    {
        DataTable salesData = FetchSalesFromDb();

        var response = await _excel.GenerateFromDataTableAsync(
            fileDownloadName: "sales-report",
            data: salesData,
            columns:
            [
                new ExcelColumnDefinition { ColumnName = "OrderId",   Header = "Order #" },
                new ExcelColumnDefinition { ColumnName = "Amount",    Header = "Total",  Format = "N2" },
                new ExcelColumnDefinition { ColumnName = "OrderDate", Header = "Date",   Format = "yyyy-MM-dd" },
                new ExcelColumnDefinition { ColumnName = "Internal",  IsHidden = true }
            ],
            worksheetName: "Sales",
            cancellationToken: ct);

        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }

    // POCO collection export (uses MiniExcel native serialisation)
    [HttpGet("products")]
    public async Task<IActionResult> ExportProducts(CancellationToken ct)
    {
        var products = await _productRepo.GetAllAsync(ct);

        var response = await _excel.GenerateFromObjectsAsync(
            fileDownloadName: "products",
            data: products,
            worksheetName: "Products",
            cancellationToken: ct);

        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }

    // Multi-sheet workbook
    [HttpGet("multi-sheet")]
    public async Task<IActionResult> ExportMultiSheet(CancellationToken ct)
    {
        var request = new ExcelReportRequest
        {
            FileDownloadName = "monthly-summary",
            Worksheets =
            [
                new ExcelWorksheetDefinition { Name = "Revenue", Data = FetchRevenue() },
                new ExcelWorksheetDefinition { Name = "Expenses", Data = FetchExpenses() },
                new ExcelWorksheetDefinition { Name = "Summary",  Data = FetchSummary() }
            ]
        };

        var response = await _excel.GenerateAsync(request, ct);
        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }
}
```

---

### ClosedXML — Quick Start

Best for presentation-quality reports with corporate branding, formulas, and user-editable output.

```csharp
using Acontplus.Reports.Interfaces;
using Acontplus.Reports.Dtos;
using Acontplus.Reports.Enums;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly IClosedXmlReportService _excel;

    public ReportController(IClosedXmlReportService excel) => _excel = excel;

    // Quick formatted export with corporate style
    [HttpGet("financial")]
    public async Task<IActionResult> FinancialReport(CancellationToken ct)
    {
        DataTable data = FetchFinancialData();

        var response = await _excel.GenerateFromDataTableAsync(
            fileDownloadName: "financial-report",
            data: data,
            columns:
            [
                new AdvancedExcelColumnDefinition
                {
                    ColumnName = "Period",
                    Header     = "Period",
                    Width      = 15,
                    Alignment  = ExcelHorizontalAlignment.Center
                },
                new AdvancedExcelColumnDefinition
                {
                    ColumnName    = "Revenue",
                    Header        = "Revenue (USD)",
                    NumberFormat  = "$#,##0.00",
                    Alignment     = ExcelHorizontalAlignment.Right,
                    AggregateType = ExcelAggregateType.Sum
                },
                new AdvancedExcelColumnDefinition
                {
                    ColumnName    = "Expenses",
                    Header        = "Expenses (USD)",
                    NumberFormat  = "$#,##0.00",
                    Alignment     = ExcelHorizontalAlignment.Right,
                    AggregateType = ExcelAggregateType.Sum
                }
            ],
            headerStyle: AdvancedExcelHeaderStyle.CorporateBlue(),
            cancellationToken: ct);

        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }

    // Full multi-sheet workbook with customised styles and aggregates
    [HttpGet("annual-report")]
    public async Task<IActionResult> AnnualReport(CancellationToken ct)
    {
        var request = new AdvancedExcelReportRequest
        {
            FileDownloadName = "annual-report-2025",
            Author           = "Finance Team",
            Company          = "Acontplus Corporation",
            Subject          = "Annual Financial Report",
            Worksheets =
            [
                new AdvancedExcelWorksheetDefinition
                {
                    Name               = "P&L Summary",
                    Data               = FetchProfitLoss(),
                    AutoFilter         = true,
                    FreezeHeaderRow    = true,
                    AlternatingRowShading = true,
                    AlternatingRowColor   = "EBF3FB",
                    IncludeAggregateRow   = true,
                    HeaderStyle           = AdvancedExcelHeaderStyle.DarkGreen(),
                    Columns =
                    [
                        new() { ColumnName = "Category", Header = "Category",    Width = 25 },
                        new() { ColumnName = "Q1",        NumberFormat = "#,##0", AggregateType = ExcelAggregateType.Sum, Alignment = ExcelHorizontalAlignment.Right },
                        new() { ColumnName = "Q2",        NumberFormat = "#,##0", AggregateType = ExcelAggregateType.Sum, Alignment = ExcelHorizontalAlignment.Right },
                        new() { ColumnName = "Q3",        NumberFormat = "#,##0", AggregateType = ExcelAggregateType.Sum, Alignment = ExcelHorizontalAlignment.Right },
                        new() { ColumnName = "Q4",        NumberFormat = "#,##0", AggregateType = ExcelAggregateType.Sum, Alignment = ExcelHorizontalAlignment.Right }
                    ]
                },
                new AdvancedExcelWorksheetDefinition
                {
                    Name   = "Balance Sheet",
                    Data   = FetchBalanceSheet(),
                    HeaderStyle = AdvancedExcelHeaderStyle.DarkGrey()
                }
            ]
        };

        var response = await _excel.GenerateAsync(request, ct);
        return File(response.FileContents, response.ContentType, response.FileDownloadName);
    }
}
```

---

### Excel DTO Reference

#### `ExcelColumnDefinition` (MiniExcel)

| Property     | Type      | Default      | Description                                                       |
| ------------ | --------- | ------------ | ----------------------------------------------------------------- |
| `ColumnName` | `string`  | _(required)_ | Source DataTable column name                                      |
| `Header`     | `string?` | `null`       | Override header label                                             |
| `Format`     | `string?` | `null`       | .NET format string applied to value (e.g. `"N2"`, `"yyyy-MM-dd"`) |
| `IsHidden`   | `bool`    | `false`      | Exclude column from output                                        |

#### `AdvancedExcelColumnDefinition` (ClosedXML)

| Property        | Type                       | Default      | Description                             |
| --------------- | -------------------------- | ------------ | --------------------------------------- |
| `ColumnName`    | `string`                   | _(required)_ | Source DataTable column name            |
| `Header`        | `string?`                  | `null`       | Override header label                   |
| `Width`         | `double?`                  | `null`       | Column width in chars (null = auto-fit) |
| `NumberFormat`  | `string?`                  | `null`       | Excel format code e.g. `"$#,##0.00"`    |
| `Alignment`     | `ExcelHorizontalAlignment` | `General`    | Cell horizontal alignment               |
| `IsBold`        | `bool`                     | `false`      | Bold data cells                         |
| `IsHidden`      | `bool`                     | `false`      | Hide column                             |
| `AggregateType` | `ExcelAggregateType`       | `None`       | Totals row formula                      |

#### `AdvancedExcelHeaderStyle` presets

```csharp
AdvancedExcelHeaderStyle.CorporateBlue()  // default — dark blue bg, white text
AdvancedExcelHeaderStyle.DarkGreen()      // forest green bg, white text
AdvancedExcelHeaderStyle.DarkGrey()       // charcoal bg, white text
AdvancedExcelHeaderStyle.LightBlue()      // pastel blue bg, navy text
AdvancedExcelHeaderStyle.Title()          // NEW v1.8.0 — white bg, dark-navy text, 14pt; used for ReportTitle / ReportSubTitle rows
AdvancedExcelHeaderStyle.GroupHeader()    // NEW v1.8.0 — mid-blue bg, white text, 10pt; used for GroupHeaders band row

// Or full customisation
new AdvancedExcelHeaderStyle
{
    BackgroundColor    = "FF5733",
    FontColor          = "FFFFFF",
    Bold               = true,
    FontSize           = 12,
    HorizontalAlignment = ExcelHorizontalAlignment.Left
};
```

#### `AdvancedExcelWorksheetDefinition` — new v1.8.0 properties

| Property           | Type                              | Default                | Description                                                                |
| ------------------ | --------------------------------- | ---------------------- | -------------------------------------------------------------------------- |
| `ReportTitle`      | `string?`                         | `null`                 | Optional merged title row rendered above all header rows.                  |
| `ReportSubTitle`   | `string?`                         | `null`                 | Optional merged subtitle row rendered directly below the title.            |
| `TitleStyle`       | `AdvancedExcelHeaderStyle?`       | `null → Title()`       | Style applied to title and subtitle rows.                                  |
| `GroupHeaders`     | `List<AdvancedExcelGroupHeader>?` | `null`                 | Band-header descriptors that span one or more data columns (Kardex-style). |
| `GroupHeaderStyle` | `AdvancedExcelHeaderStyle?`       | `null → GroupHeader()` | Style applied to the group-header band row.                                |

#### `AdvancedExcelGroupHeader` (new v1.8.0)

| Property           | Type              | Description                                        |
| ------------------ | ----------------- | -------------------------------------------------- |
| `Title`            | required `string` | Label displayed in the merged band cell.           |
| `StartColumnIndex` | required `int`    | First column (1-based, inclusive) the band covers. |
| `EndColumnIndex`   | required `int`    | Last column (1-based, inclusive) the band covers.  |

> **Row order with title/subtitle/group headers:**
> `ReportTitle` row → `ReportSubTitle` row → `GroupHeaders` row → Column header row → Data rows → Aggregate totals row.
> `FreezeRows` and `SetAutoFilter` are automatically applied to the column header row, regardless of how many rows precede it.

##### ClosedXML Kardex example

```csharp
new AdvancedExcelWorksheetDefinition
{
    Name            = "Kardex",
    Data            = kardexTable,
    ReportTitle     = "KARDEX — PRD-007: SSD 1TB NVMe",
    ReportSubTitle  = "Período: Enero 2026  |  Método: Promedio Ponderado",
    TitleStyle      = AdvancedExcelHeaderStyle.Title(),
    GroupHeaders    =
    [
        new AdvancedExcelGroupHeader { Title = "ENTRADAS", StartColumnIndex = 4, EndColumnIndex = 6 },
        new AdvancedExcelGroupHeader { Title = "SALIDAS",  StartColumnIndex = 7, EndColumnIndex = 9 },
        new AdvancedExcelGroupHeader { Title = "SALDO",    StartColumnIndex = 10, EndColumnIndex = 12 }
    ],
    GroupHeaderStyle  = AdvancedExcelHeaderStyle.GroupHeader(),
    AutoFilter        = true,
    FreezeHeaderRow   = true,
    IncludeAggregateRow = true,
    Columns = [ /* 12 column definitions */ ]
}
```

#### `ExcelAggregateType` values

`None` · `Sum` · `Average` · `Count` · `CountA` · `Min` · `Max`

---

## �📚 Advanced Configuration

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

---

## 📋 Changelog

### v1.8.0

- **New** `QuestPdfSectionType.InvoiceHeader` — SRI Ecuador–style invoice header section (`QuestPdfInvoiceHeader` DTO): company block (left), SRI auth box with border (right), buyer band (bottom)
- **New** `QuestPdfSectionType.Image` — raw `byte[]` image section with configurable max dimensions and alignment
- **New** `QuestPdfSectionType.Barcode` — Code-128 / QR code section generated from text via `Acontplus.Barcode` (`QuestPdfBarcodeType` enum)
- **New** `QuestPdfSectionType.MasterDetail` — master rows each followed by a filtered detail sub-table (EstadoCuenta / Statement pattern)
- **New** `QuestPdfSectionType.TwoColumn` — side-by-side layout with independent left/right content, configurable column ratios and gap
- **New** `QuestPdfTableColumn.IsGroupHeader` / `ColumnSpan` — grouped / band header rows in DataTable (Kardex pattern: Entradas | Salidas | Saldo)
- **New** `QuestPdfHeaderFooterOptions.LogoBytes` + `LogoMimeType` — logo from `byte[]` instead of file path (database-sourced logos)
- **New** `QuestPdfDocumentSettings.WatermarkFontSize` + `WatermarkColor` — configurable watermark appearance
- **New** `AdvancedExcelWorksheetDefinition.ReportTitle`, `ReportSubTitle`, `TitleStyle`, `GroupHeaders`, `GroupHeaderStyle` — title, subtitle and band-header rows above column headers (ClosedXML)
- **New** `AdvancedExcelGroupHeader` DTO — describes a merge-span band header (`Title`, `StartColumnIndex`, `EndColumnIndex`)
- **New** `AdvancedExcelHeaderStyle.Title()` and `GroupHeader()` presets
- **Demo** — six new endpoints in `ReportsEndpoints.cs`: `sri-invoice`, `barcode`, `master-detail`, `kardex`, `two-column`, `closedxml/grouped-report`

### v1.7.0

- **New** `IMiniExcelReportService` / `MiniExcelReportService` — high-performance streaming Excel exports (MiniExcel 1.42.0)
  - `GenerateAsync(ExcelReportRequest)` — single and multi-sheet workbooks from DataTable sources
  - `GenerateFromDataTableAsync(...)` — convenience single-table shortcut with column visibility, header overrides, and format hints
  - `GenerateFromObjectsAsync<T>(...)` — strongly-typed POCO collection export using native MiniExcel serialisation
- **New** `IClosedXmlReportService` / `ClosedXmlReportService` — richly formatted Excel workbooks (ClosedXML 0.105.0)
  - `GenerateAsync(AdvancedExcelReportRequest)` — full multi-sheet workbooks with metadata
  - `GenerateFromDataTableAsync(...)` — convenience shortcut with per-column formatting and header style
  - Corporate header styles (CorporateBlue, DarkGreen, DarkGrey, LightBlue)
  - Freeze panes, AutoFilter, alternating row shading, aggregate formula totals rows
- **New** DTOs: `ExcelColumnDefinition`, `ExcelWorksheetDefinition`, `ExcelReportRequest`
- **New** DTOs: `AdvancedExcelColumnDefinition`, `AdvancedExcelHeaderStyle`, `AdvancedExcelWorksheetDefinition`, `AdvancedExcelReportRequest`
- **New** Enums: `ExcelHorizontalAlignment`, `ExcelAggregateType`
- **New** DI extensions: `AddMiniExcelReportService()`, `AddClosedXmlReportService()`
- `AddReportServices()` now registers all four services (RDLC + QuestPDF + MiniExcel + ClosedXML)

### v1.6.0

- Added `IQuestPdfReportService` / `QuestPdfReportService` — QuestPDF dynamic PDF generation with fluent composition, multi-section layouts, typed DataTable tables, key-value panels, aggregate totals, full theming, watermarks, and page-number footers

### v1.5.x

- RDLC report generation performance improvements
- Concurrency control and timeout protection
- Report definition caching
- Direct printing support for thermal printers
