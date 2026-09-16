# Changelog

All notable changes to Acontplus .NET Libraries are documented here.
See individual package READMEs for installation and usage details.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions.
Versioning follows [Semantic Versioning](https://semver.org/).

---

## Acontplus.Analytics

### [1.0.14]

- **Added** Comprehensive analytics and statistics DTOs (`DashboardStatistics`, `RealTimeMetrics`, `AggregatedAnalytics`, `TrendAnalysis`).
- **Added** Domain-agnostic business intelligence models and comparison metrics (period-over-period, year-over-year).
- **Added** SQL template library support for reusable database reporting queries.
- **Fixed** Security vulnerabilities and dependency hygiene across transitive packages.

---

## Acontplus.ApiDocumentation

### [1.3.0]

- **Added** Full Minimal API versioning support via `app.NewApiVersionSet()` and `WithApiVersionSet()`.
- **Added** Swagger UI version dropdown integration via `app.DescribeApiVersions()`.
- **Added** Pre-configured JWT Bearer authentication UI in Swagger.
- **Added** XML documentation file aggregation across all loaded assemblies in the output path.
- **Added** Configurable API metadata (contact, license, terms of service) from `appsettings.json`.

---

## Acontplus.Barcode

### [1.2.11]

- **Added** High-performance 1D/2D barcode generation using SkiaSharp and ZXing.Net.
- **Added** Supported formats: QR Code, Code 128, Code 39, EAN-13, EAN-8, UPC-A, UPC-E, PDF417, and Data Matrix.
- **Added** Custom foreground and background color customization via `SKColor`, configurable margins, and error correction levels.
- **Fixed** Memory allocation and image disposal during high-throughput barcode rendering.

---

## Acontplus.Billing

### [1.3.8]

- **Fixed** Vulnerability remediation and transitive dependency updates.
- **Improved** XML namespace handling and signature validation for SRI electronic invoices.

### [1.3.0] - 2026-05-30

- **Added** `ISriSigner` — interface contract for XAdES-BES XML signature.
- **Added** `SriSigner` — native implementation of XAdES-BES electronic signature for SRI Ecuador, replicating MITyCLibXADES output expected by the SRI web service:
  - RSA-SHA1 (`xmldsig#rsa-sha1`) with SHA-1 digests throughout.
  - Three-reference structure: `etsi:SignedProperties` | `ds:KeyInfo` | comprobante (enveloped).
  - Full `ds:KeyInfo` with `X509Data` and `RSAKeyValue` blocks.
  - `etsi:SigningCertificate` with SHA-1 cert digest and decimal `X509SerialNumber`.
  - `etsi:DataObjectFormat` with MIME type `text/xml`.
  - Cryptographically secure random IDs via `RandomNumberGenerator.GetInt32`.
  - `EphemeralKeySet` flag prevents PFX key material from being written to disk (required on Linux/macOS).
  - Cross-platform timezone resolution: IANA `America/Guayaquil` → Windows `SA Pacific Standard Time` → fixed UTC-5 fallback.
  - No additional NuGet dependency — uses `System.Security.Cryptography.Xml`.
- **Changed** `Sign` API returns `string` instead of `bool` + `ref` out-parameter; failures surface as typed exceptions (`ArgumentException`, `InvalidOperationException`).

---

## Acontplus.Core

### [2.4.0]

- **Changed** `Result` and `Result<T>` pattern refactored with immutable record struct semantics, unified error propagation, and functional factory methods.
- **Added** `IDomainEventHandler<TEvent>` async contract for decoupled in-process domain events.
- **Added** Enhanced async specification queries and pagination helpers in `FilterRequestExtensions` and `FilterPredicateExtensions`.
- **Security** Hardened `XmlValidator` against XML External Entity (XXE) injection attacks.
- **Fixed** Standardized HTTP problem details mapping across `ApiException`, `DomainException`, and `ErrorTypeExtensions`.

### [2.3.0]

- **Fixed** `EntityCreatedEvent`: parameter renamed `DeletedByUserId` → `CreatedByUserId`.
- **Fixed** `EntityModifiedEvent`: parameter renamed `DeletedByUserId` → `ModifiedByUserId`.
- **Fixed** `EntityRestoredEvent`: parameter renamed `DeletedByUserId` → `RestoredByUserId`.
- **Fixed** `ErrorType.Timeout` HTTP mapping corrected to 504 Gateway Timeout (was wrongly documented as 408; `RequestTimeout` is the 408 type).
- **Changed** `JsonExtensions.DefaultOptions`, `PrettyOptions`, `StrictOptions` — converted from property to `static readonly` field for improved metadata caching.
- **Changed** `PagedResult<T>` — converted from mutable `class` with `set` properties to `sealed record` with `init`-only properties; `Metadata` type widened from `Dictionary<string,object>` to `IReadOnlyDictionary<string,object>`.
- **Changed** `SpResponse` — `dynamic` replaced by `object?`; `Payload` marked `[Obsolete]`; `IsSuccess` simplified to `Code == "0"` only.
- **Added** `IHttpClientService` — new abstraction at `Abstractions/Infrastructure/Http/` for decoupled outbound HTTP with header and `CancellationToken` support.
- **Added** `ICacheService.RemoveByPrefix`, `RemoveByPrefixAsync` — batch cache invalidation by key prefix.
- **Added** `ICacheService.GetStatisticsAsync` — exposes the existing `CacheStatistics` class.
- **Added** `DataValidation.IsValidEmail`, `IsValidUrl`, `IsValidPhoneNumber` with precompiled regex and timeout guards.
- **Added** Full XML documentation on all public types and members.
- **Changed** Target framework: `net10.0` only.

---

## Acontplus.Infrastructure

### [1.4.0]

- **Added** Unified `ICacheService` supporting both in-memory cache and distributed Redis cache with automatic fallback.
- **Added** Polly resilience policies: circuit breakers, exponential backoff retries, and operation timeouts.
- **Added** Preconfigured resilient `HttpClient` factory integration using `Microsoft.Extensions.Http.Resilience`.
- **Added** Rate limiting, health check registrations, and response compression middleware extensions.
- **Added** In-memory event bus implementation for asynchronous domain event dispatching.

---

## Acontplus.Logging

### [2.1.0]

- **Added** Extended Serilog and OpenTelemetry enrichment for distributed tracing and container environments.
- **Fixed** Cleaned up unused dependencies and aligned target framework to `net10.0`.

### [2.0.0]

- **Breaking** Removed per-signal OTLP properties (`EnableOtlpExporter`, `OtlpEndpoint`, `OtlpProtocol`) from `TracingOptions`, `MetricsOptions`, and `OTelLoggingOptions`; configure once at `OpenTelemetryOptions` root instead.
- **Breaking** Removed `OTelLoggingOptions.Enabled` — logging OTLP is now governed by `OpenTelemetryOptions.EnableOtlpExporter`.
- **Added** `OpenTelemetryOptions.EnableOtlpExporter` — single flag enabling OTLP for all three signals (traces, metrics, logs).
- **Added** `OpenTelemetryOptions.OtlpEndpoint` / `OtlpProtocol` — shared endpoint and protocol used by all signals.
- **Changed** OTLP registration strategy: uses `UseOtlpExporter(protocol, endpoint)` (one call for all signals) when no Dynatrace exporter is active; falls back to per-signal `AddOtlpExporter` automatically when Dynatrace is configured.
- **Added** Full OpenTelemetry support: distributed tracing (`OpenTelemetry.Instrumentation.AspNetCore`, `Http`, `SqlClient`), metrics, and OTLP log export.
- **Added** Dynatrace exporter support via per-signal OTLP with `Authorization: Api-Token` header injection.
- **Added** `TracingOptions.AdditionalSources` — register extra `ActivitySource` names from configuration.
- **Added** `MetricsOptions.AdditionalMeters` — register extra `Meter` names from configuration.
- **Added** Auto-detection of `ServiceName` and `ServiceVersion` from entry assembly metadata.
- **Added** `TracingHelper` and `MetricsHelper` convenience wrappers for DI injection.

---

## Acontplus.Notifications

### [1.6.11]

- **Fixed** Vulnerability remediation, resilient connection pooling, and client timeout guards.

### [1.6.0]

- **Added** WhatsApp Cloud API integration (Meta Graph API v23.0).
- **Added** Text messages — plain text with URL preview, in-thread replies.
- **Added** Templates — body/header params, image/video/document headers, quick-reply & URL buttons.
- **Added** Media — image, document, audio, video, sticker (URL or pre-uploaded ID).
- **Added** Location — map pin with name and address.
- **Added** Interactive — quick-reply buttons, scrollable list menu, CTA URL button.
- **Added** Reactions — send/remove emoji reactions on received messages.
- **Added** Read receipts — mark messages as read (double blue ticks).
- **Added** Media upload — upload files once, reuse the ID in multiple messages.
- **Added** Webhook validation — HMAC-SHA256 signature verification (`X-Hub-Signature-256`).
- **Added** Multi-tenant support — default account + named accounts per company + per-request inline override.
- **Added** Built-in resilience — 3 retries, exponential back-off, circuit breaker, per-attempt timeout.

### [1.5.0]

- **Added** Template caching — 50x faster template loading (30-min memory cache, sliding expiration).
- **Improved** 99% less I/O — cached templates eliminate repeated disk reads.
- **Changed** Backward-compatible — optional `IMemoryCache` injection.

---

## Acontplus.Persistence.Common

### [2.2.0]

- **Added** Centralized `AuditSaveChangesInterceptor` for EF Core, extracting audit entity population into persistence abstractions.
- **Added** Shared `BaseAdoRepository` and `BaseDapperRepository` base implementations for micro-ORM queries.
- **Added** `EntityRegistrationDispatcher` for automated entity and model configuration discovery across assemblies.
- **Added** `UnitOfWork` pattern contract and default implementation.
- **Added** Hierarchical `IConnectionStringProvider` with environment-specific overrides.

---

## Acontplus.Persistence.PostgreSQL

### [1.4.7]

- **Fixed** Vulnerability remediation, dependency alignment, and connection string parsing improvements.

### [1.4.0]

- **Added** `AuditSaveChangesInterceptor` — EF Core `SaveChangesInterceptor` that automatically populates audit fields (`CreatedBy`, `CreatedByUserId`, `IsMobileRequest`, `UpdatedBy`, `UpdatedByUserId`, `DeletedBy`, `DeletedByUserId`) on every `BaseEntity` before saving.
- **Fixed** Audit identity fields now correctly captured per-request under `AddDbContextPool`. The old constructor-injection approach captured the first request's user permanently on each pooled context instance; `CreateScope()` per save resolves a fresh `IAuditContext` every time.
- **Changed** `AddPostgresPersistence<TContext>` now registers `AuditSaveChangesInterceptor` as a singleton and injects it into the `DbContextPool` factory; requires no extra configuration.
- **Changed** `BaseContext` — removed `IAuditContext` constructor overloads; timestamps (`CreatedAt`, `UpdatedAt`, `DeletedAt`) are still managed by `BaseContext`; user-identity fields are now exclusively handled by `AuditSaveChangesInterceptor`.
- **Changed** Audit stamping is a no-op when `IAuditContext` is not registered in DI.

---

## Acontplus.Persistence.SqlServer

### [2.2.7]

- **Fixed** Vulnerability remediation, dependency alignment, and query retry policy tuning.

### [2.2.0]

- **Added** `AuditSaveChangesInterceptor` — EF Core `SaveChangesInterceptor` that automatically populates audit fields (`CreatedBy`, `CreatedByUserId`, `IsMobileRequest`, `UpdatedBy`, `UpdatedByUserId`, `DeletedBy`, `DeletedByUserId`) on every `BaseEntity` before saving.
- **Fixed** Audit identity fields now correctly captured per-request under `AddDbContextPool`. The old constructor-injection approach captured the first request's user permanently on each pooled context instance; `CreateScope()` per save resolves a fresh `IAuditContext` every time.
- **Changed** `AddSqlServerPersistence<TContext>` now registers `AuditSaveChangesInterceptor` as a singleton and injects it into the `DbContextPool` factory; requires no extra configuration.
- **Changed** `BaseContext` — removed `IAuditContext` constructor overloads; timestamps (`CreatedAt`, `UpdatedAt`, `DeletedAt`) are still managed by `BaseContext`; user-identity fields are now exclusively handled by `AuditSaveChangesInterceptor`.
- **Changed** Audit stamping is a no-op when `IAuditContext` is not registered in DI.

---

## Acontplus.Reports

### [1.8.1]

- **Fixed** Font fallback handling and Linux container rendering compatibility for QuestPDF.

### [1.8.0]

- **Added** `QuestPdfSectionType.InvoiceHeader` — SRI Ecuador-style invoice header section (`QuestPdfInvoiceHeader` DTO): company block (left), SRI auth box with border (right), buyer band (bottom).
- **Added** `QuestPdfSectionType.Image` — raw `byte[]` image section with configurable max dimensions and alignment.
- **Added** `QuestPdfSectionType.Barcode` — Code-128 / QR code section generated from text via `Acontplus.Barcode` (`QuestPdfBarcodeType` enum).
- **Added** `QuestPdfSectionType.MasterDetail` — master rows each followed by a filtered detail sub-table (EstadoCuenta / Statement pattern).
- **Added** `QuestPdfSectionType.TwoColumn` — side-by-side layout with independent left/right content, configurable column ratios and gap.
- **Added** `QuestPdfTableColumn.IsGroupHeader` / `ColumnSpan` — grouped / band header rows in DataTable (Kardex pattern: Entradas | Salidas | Saldo).
- **Added** `QuestPdfHeaderFooterOptions.LogoBytes` + `LogoMimeType` — logo from `byte[]` instead of file path (database-sourced logos).
- **Added** `QuestPdfDocumentSettings.WatermarkFontSize` + `WatermarkColor` — configurable watermark appearance.
- **Added** `AdvancedExcelWorksheetDefinition.ReportTitle`, `ReportSubTitle`, `TitleStyle`, `GroupHeaders`, `GroupHeaderStyle` — title, subtitle, and band-header rows above column headers (ClosedXML).
- **Added** `AdvancedExcelGroupHeader` DTO — describes a merge-span band header (`Title`, `StartColumnIndex`, `EndColumnIndex`).
- **Added** `AdvancedExcelHeaderStyle.Title()` and `GroupHeader()` presets.
- **Added** Demo endpoints in `ReportsEndpoints.cs`: `sri-invoice`, `barcode`, `master-detail`, `kardex`, `two-column`, `closedxml/grouped-report`.

### [1.7.0]

- **Added** `IMiniExcelReportService` / `MiniExcelReportService` — high-performance streaming Excel exports (MiniExcel 1.42.0).
  - `GenerateAsync(ExcelReportRequest)` — single and multi-sheet workbooks from DataTable sources.
  - `GenerateFromDataTableAsync(...)` — convenience single-table shortcut with column visibility, header overrides, and format hints.
  - `GenerateFromObjectsAsync<T>(...)` — strongly-typed POCO collection export using native MiniExcel serialisation.
- **Added** `IClosedXmlReportService` / `ClosedXmlReportService` — richly formatted Excel workbooks (ClosedXML 0.105.0).
  - `GenerateAsync(AdvancedExcelReportRequest)` — full multi-sheet workbooks with metadata.
  - `GenerateFromDataTableAsync(...)` — convenience shortcut with per-column formatting and header style.
  - Corporate header styles (CorporateBlue, DarkGreen, DarkGrey, LightBlue).
  - Freeze panes, AutoFilter, alternating row shading, aggregate formula totals rows.
- **Added** DTOs: `ExcelColumnDefinition`, `ExcelWorksheetDefinition`, `ExcelReportRequest`.
- **Added** DTOs: `AdvancedExcelColumnDefinition`, `AdvancedExcelHeaderStyle`, `AdvancedExcelWorksheetDefinition`, `AdvancedExcelReportRequest`.
- **Added** Enums: `ExcelHorizontalAlignment`, `ExcelAggregateType`.
- **Added** DI extensions: `AddMiniExcelReportService()`, `AddClosedXmlReportService()`.
- **Changed** `AddReportServices()` now registers all four services (RDLC + QuestPDF + MiniExcel + ClosedXML).

### [1.6.0]

- **Added** `IQuestPdfReportService` / `QuestPdfReportService` — QuestPDF dynamic PDF generation with fluent composition, multi-section layouts, typed DataTable tables, key-value panels, aggregate totals, full theming, watermarks, and page-number footers.

### [1.5.0]

- **Improved** RDLC report generation performance.
- **Added** Concurrency control and timeout protection.
- **Added** Report definition caching.
- **Added** Direct printing support for thermal printers.

---

## Acontplus.S3Application

### [2.1.0]

- **Fixed** Memory leak prevention on client disposal and Polly retry policy backoff tuning.

### [2.0.0]

- **Added** Connection pooling — reuses S3 clients per credentials/region (25x faster).
- **Added** Polly retry policy — automatic exponential backoff for transient failures.
- **Added** Rate limiting — prevents AWS throttling (configurable requests/second).
- **Added** Configurable resilience — customize timeouts, retries, and delays.
- **Added** Structured logging — detailed operation metrics and diagnostics.
- **Changed** Thread-safe client management via `ConcurrentDictionary` for multi-threaded workloads.
- **Changed** Service registration now requires DI (`services.AddS3Storage(configuration)`).
- **Added** `IDisposable` — proper resource cleanup and disposal.
- **Performance** Connections/second: 10-20 → 100-500 (25x faster); Memory/request: ~5MB → ~500KB (90% reduction); Avg latency: 150-300ms → 50-100ms (66% faster).
- **Breaking** Service registration changed from direct instantiation to DI (`services.AddS3Storage(configuration)`).

---

## Acontplus.Services

### [2.5.0]

- **Fixed** Security vulnerabilities, JWT claims validation hardening, and token expiration handling.

### [2.4.0] - 2026-08-10

- **Added** opt-in antiforgery support for cookie-authenticated web flows: `AddAntiforgerySupport()`, `UseAntiforgerySupport()`, and optional `MapAntiforgeryTokenEndpoint()`.
- **Added** safe warning-level observability for rejected antiforgery requests; token values and request bodies are never logged.
- **Changed** protection remains endpoint-scoped: APIs select protected routes/groups and explicitly leave webhooks, machine-to-machine endpoints, and bearer-only APIs outside CSRF validation.
- **Changed** JWT audience configuration rejects missing or blank values; access-token issuance now requires one explicit resource audience.

---

## Acontplus.Utilities

### [2.3.0]

- **Added** Constructor argument mapping resolution for records and classes in `ExpressionBuilder`.
- **Fixed** Duplicate method declaration in mapper expression tree builder.
- **Security** Hardened regex evaluation against catastrophic backtracking (ReDoS) across string utilities.

### [2.2.0] - 2026-06-24

- **Added** `IObjectMapper` — public interface for compiled-delegate object mapping; register via `services.AddObjectMapper()`.
- **Added** `MappingProfile` — abstract base class for grouping `CreateMap<TSource, TTarget>()` registrations with fluent `ForCtorParam`, `Ignore`, and `ReverseMap` configuration.
- **Added** `MappingExpression<TSource, TTarget>` — fluent builder returned by `CreateMap`; supports `ForMember` (expression overload), `Ignore`, `ForCtorParam`, and `ReverseMap`.
- **Added** `MapperConfiguration` — collects profiles, validates all type pairs in a single pass, and compiles delegates at startup; sealed after `Build()` to prevent late registration.
- **Added** `MapperRegistry` — thread-safe `ConcurrentDictionary<TypePair, Lazy<Delegate>>` backing store; convention pairs compiled once on first use.
- **Added** `TypePair` — `readonly struct` dictionary key with `IEquatable<TypePair>` and `HashCode.Combine` hash.
- **Added** `ExpressionBuilder` (internal) — builds `LambdaExpression` trees for flat, nested, collection, and constructor-parameter mappings; cycle detection via in-progress `HashSet<TypePair>`.
- **Added** `DelegateCompiler` (internal) — compiles `LambdaExpression` to `Delegate`; handles convention-only pairs on demand.
- **Added** `UtilitiesServiceExtensions.AddObjectMapper(params MappingProfile[])` — registers `IObjectMapper` as `ServiceLifetime.Singleton`; zero profiles accepted for full convention-based on-demand mapping.
- **Added** `FilterQueryExtensions.ToPaginationRequest()` — converts `PaginationQuery` to `PaginationRequest` without external mapper dependency.
- **Added** `FilterQueryExtensions.ToFilterRequest()` — converts `FilterQuery` to `FilterRequest` without external mapper dependency.
- **Removed** `ObjectMapper` static class — the reflection-based static API has been deleted; all call sites must use injected `IObjectMapper`.
- **Changed** `PackageTags` updated to include `mapping`, `expression-trees`, `compiled-delegates`, `object-mapper`.
- **Changed** README rewritten to document the new DI-based mapper API, profile authoring guide, and updated pagination conversion examples.
