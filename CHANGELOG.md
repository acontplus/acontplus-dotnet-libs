# Changelog

All notable changes to Acontplus .NET Libraries are documented here.
See individual package READMEs for installation and usage details.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions.
Versioning follows [Semantic Versioning](https://semver.org/).

---

## Acontplus.Reports

### [1.8.0]

- **Added** `QuestPdfSectionType.InvoiceHeader` — SRI Ecuador-style invoice header section (`QuestPdfInvoiceHeader` DTO): company block (left), SRI auth box with border (right), buyer band (bottom)
- **Added** `QuestPdfSectionType.Image` — raw `byte[]` image section with configurable max dimensions and alignment
- **Added** `QuestPdfSectionType.Barcode` — Code-128 / QR code section generated from text via `Acontplus.Barcode` (`QuestPdfBarcodeType` enum)
- **Added** `QuestPdfSectionType.MasterDetail` — master rows each followed by a filtered detail sub-table (EstadoCuenta / Statement pattern)
- **Added** `QuestPdfSectionType.TwoColumn` — side-by-side layout with independent left/right content, configurable column ratios and gap
- **Added** `QuestPdfTableColumn.IsGroupHeader` / `ColumnSpan` — grouped / band header rows in DataTable (Kardex pattern: Entradas | Salidas | Saldo)
- **Added** `QuestPdfHeaderFooterOptions.LogoBytes` + `LogoMimeType` — logo from `byte[]` instead of file path (database-sourced logos)
- **Added** `QuestPdfDocumentSettings.WatermarkFontSize` + `WatermarkColor` — configurable watermark appearance
- **Added** `AdvancedExcelWorksheetDefinition.ReportTitle`, `ReportSubTitle`, `TitleStyle`, `GroupHeaders`, `GroupHeaderStyle` — title, subtitle, and band-header rows above column headers (ClosedXML)
- **Added** `AdvancedExcelGroupHeader` DTO — describes a merge-span band header (`Title`, `StartColumnIndex`, `EndColumnIndex`)
- **Added** `AdvancedExcelHeaderStyle.Title()` and `GroupHeader()` presets
- **Added** Demo endpoints in `ReportsEndpoints.cs`: `sri-invoice`, `barcode`, `master-detail`, `kardex`, `two-column`, `closedxml/grouped-report`

### [1.7.0]

- **Added** `IMiniExcelReportService` / `MiniExcelReportService` — high-performance streaming Excel exports (MiniExcel 1.42.0)
  - `GenerateAsync(ExcelReportRequest)` — single and multi-sheet workbooks from DataTable sources
  - `GenerateFromDataTableAsync(...)` — convenience single-table shortcut with column visibility, header overrides, and format hints
  - `GenerateFromObjectsAsync<T>(...)` — strongly-typed POCO collection export using native MiniExcel serialisation
- **Added** `IClosedXmlReportService` / `ClosedXmlReportService` — richly formatted Excel workbooks (ClosedXML 0.105.0)
  - `GenerateAsync(AdvancedExcelReportRequest)` — full multi-sheet workbooks with metadata
  - `GenerateFromDataTableAsync(...)` — convenience shortcut with per-column formatting and header style
  - Corporate header styles (CorporateBlue, DarkGreen, DarkGrey, LightBlue)
  - Freeze panes, AutoFilter, alternating row shading, aggregate formula totals rows
- **Added** DTOs: `ExcelColumnDefinition`, `ExcelWorksheetDefinition`, `ExcelReportRequest`
- **Added** DTOs: `AdvancedExcelColumnDefinition`, `AdvancedExcelHeaderStyle`, `AdvancedExcelWorksheetDefinition`, `AdvancedExcelReportRequest`
- **Added** Enums: `ExcelHorizontalAlignment`, `ExcelAggregateType`
- **Added** DI extensions: `AddMiniExcelReportService()`, `AddClosedXmlReportService()`
- **Changed** `AddReportServices()` now registers all four services (RDLC + QuestPDF + MiniExcel + ClosedXML)

### [1.6.0]

- **Added** `IQuestPdfReportService` / `QuestPdfReportService` — QuestPDF dynamic PDF generation with fluent composition, multi-section layouts, typed DataTable tables, key-value panels, aggregate totals, full theming, watermarks, and page-number footers

### [1.5.x]

- **Improved** RDLC report generation performance
- **Added** Concurrency control and timeout protection
- **Added** Report definition caching
- **Added** Direct printing support for thermal printers

---

## Acontplus.Notifications

### [1.6.0] — WhatsApp Cloud API (Meta Graph API v23.0)

- **Added** Text messages — plain text with URL preview, in-thread replies
- **Added** Templates — body/header params, image/video/document headers, quick-reply & URL buttons
- **Added** Media — image, document, audio, video, sticker (URL or pre-uploaded ID)
- **Added** Location — map pin with name and address
- **Added** Interactive — quick-reply buttons, scrollable list menu, CTA URL button
- **Added** Reactions — send/remove emoji reactions on received messages
- **Added** Read receipts — mark messages as read (double blue ticks)
- **Added** Media upload — upload files once, reuse the ID in multiple messages
- **Added** Webhook validation — HMAC-SHA256 signature verification (X-Hub-Signature-256)
- **Added** Multi-tenant support — default account + named accounts per company + per-request inline override
- **Added** Built-in resilience — 3 retries, exponential back-off, circuit breaker, per-attempt timeout

### [1.5.0] — Email Performance & Caching

- **Added** Template caching — 50x faster template loading (30-min memory cache, sliding expiration)
- **Improved** 99% less I/O — cached templates eliminate repeated disk reads
- **Changed** Backward-compatible — optional `IMemoryCache` injection

---

## Acontplus.S3Application

### [2.0.0] — Scalability & Resilience

- **Added** Connection pooling — reuses S3 clients per credentials/region (25x faster)
- **Added** Polly retry policy — automatic exponential backoff for transient failures
- **Added** Rate limiting — prevents AWS throttling (configurable requests/second)
- **Added** Configurable resilience — customize timeouts, retries, and delays
- **Added** Structured logging — detailed operation metrics and diagnostics
- **Changed** Thread-safe client management via `ConcurrentDictionary` for multi-threaded workloads
- **Changed** Service registration now requires DI (`services.AddS3Storage(configuration)`)
- **Added** `IDisposable` — proper resource cleanup and disposal
- **Performance** Connections/second: 10-20 → 100-500 (25x faster); Memory/request: ~5MB → ~500KB (90% reduction); Avg latency: 150-300ms → 50-100ms (66% faster)

> **Breaking change (v1.x → v2.0.0):** Service registration changed from direct instantiation to DI.
> Migration: replace `services.AddScoped<IS3StorageService, S3StorageService>()` with `services.AddS3Storage(configuration)`.
