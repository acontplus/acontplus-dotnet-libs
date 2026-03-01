using Acontplus.Core.Domain.Common.Events;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.S3Application.Extensions;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
using Demo.Api.Endpoints.Business.Analytics;
using Demo.Api.Endpoints.Demo;
using Demo.Api.Endpoints.Infrastructure;
using Demo.Application.Services;
using Demo.Infrastructure.EventHandlers;
using Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Scrutor;

namespace Demo.Api.Extensions;

/// <summary>
/// Consolidated extension methods for configuring the Demo API application.
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// Adds all services required for the Demo API application.
    /// </summary>
    public static IServiceCollection AddAllDemoServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Demo API specific services (HTTP context, OpenAPI, JSON config)
        services.AddHttpContextAccessor();
        services.AddOpenApi();
        services.AddAntiforgery();
        JsonConfigurationService.ConfigureAspNetCore(services, useStrictMode: false);
        JsonConfigurationService.RegisterJsonConfiguration(services);

        // Configure Infrastructure Services (v2.0) with health checks and response compression
        services.AddInfrastructureServices(configuration, addHealthChecks: true, addResponseCompression: true);

        // Configure Application Services (v2.0)
        services.AddApplicationServices(configuration);

        // Configure Lookup Service
        services.AddLookupService();

        // Configure Report Services
        services.AddReportServices(configuration);

        // ========================================
        // S3 STORAGE SERVICES (v2.0.0)
        // ========================================
        // Features: Connection pooling, Polly retry, rate limiting
        services.AddS3Storage(configuration);

        // ========================================
        // EMAIL NOTIFICATION SERVICES (v1.5.0)
        // ========================================
        // Optional: Enable template caching for better performance
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Adds database services for SQL Server persistence.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Registro para la base de datos principal de la aplicación
        // IUnitOfWork is automatically registered and provides GetRepository<T>() method
        // No need to register individual repositories - UoW handles it
        services.AddSqlServerPersistence<TestContext>(sqlServerOptions =>
        {
            // Configure SQL Server options
            sqlServerOptions.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("Demo.Infrastructure"));
        });

        // Configure persistence resilience from appsettings.json
        // This enables dynamic retry policies, circuit breakers, and timeouts for ADO repositories
        services.Configure<PersistenceResilienceOptions>(
            configuration.GetSection(PersistenceResilienceOptions.SectionName));

        // ========================================
        // DAPPER REPOSITORY (v2.2.0)
        // ========================================
        // High-performance raw SQL queries with Dapper
        // Uses same connection string and resilience options as EF Core
        services.AddSqlServerDapperRepository();

        // Register Dapper-based reporting service
        services.AddScoped<IOrderReportService, OrderReportService>();

        return services;
    }

    /// <summary>
    /// Adds business services using assembly scanning.
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        string[] nameSpaces =
        [
            "Acontplus.Reports.Services",
            "Acontplus.Utilities.Security.Services",
            "Demo.Application.Services",
        ];

        services.Scan(scan => scan
            .FromApplicationDependencies()
            .AddClasses(classes => classes.InNamespaces(nameSpaces))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        services.AddScoped<IAdoRepository, AdoRepository>();
        services.AddScoped<IAtsXmlService, AtsXmlService>();
        services.AddScoped<IWebServiceSri, WebServiceSri>();
        services.AddScoped<IRucService, RucService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<ICedulaService, CedulaService>();
        services.AddScoped<IXmlSriFileService, XmlSriFileService>();
        services.AddScoped<IMailKitService, AmazonSesService>();
        services.AddTransient<ISqlExceptionTranslator, SqlExceptionTranslator>();
        services.AddDataProtection();

        // ========================================
        // EVENT SYSTEMS CONFIGURATION
        // ========================================

        // 1. DOMAIN EVENT DISPATCHER (Transactional, Synchronous)
        // Use for: Operations where second insert depends on first insert's ID
        // Runs in same transaction/UoW - if handler fails, entire transaction rolls back
        services.AddDomainEventDispatcher();
        services.AddDomainEventHandler<EntityCreatedEvent, EntityAuditHandler>();
        services.AddDomainEventHandler<EntityCreatedEvent, OrderLineItemsCreationHandler>();

        // 2. APPLICATION EVENT BUS (Async, Background Processing)
        // Use for: Cross-service communication, notifications, analytics, integration
        // Runs asynchronously in background - eventual consistency
        services.AddInMemoryEventBus(options =>
        {
            options.EnableDiagnosticLogging = true;
        });

        // Register Application Event Handlers as Background Services
        services.AddHostedService<OrderNotificationHandler>();
        services.AddHostedService<OrderAnalyticsHandler>();
        services.AddHostedService<OrderWorkflowHandler>();

        // ========================================
        // APPLICATION SERVICES
        // ========================================
        services.AddScoped<IOrderService, OrderService>();

        // ========================================
        // ANALYTICS SERVICES
        // ========================================
        services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();

        return services;
    }

    /// <summary>
    /// Configures the middleware pipeline for the Demo API.
    /// </summary>
    public static void ConfigureDemoApiMiddleware(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        // Use Serilog request logging BEFORE other middleware like UseRouting, UseAuthentication, etc.
        app.UseSerilogRequestLogging(); // Captures HTTP request/response details

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // NOTE: UseApiVersioningAndDocumentation() is intentionally NOT called here.
            // For Minimal APIs, the IApiVersionDescriptionProvider requires all endpoint
            // version sets to be registered first (via app.NewApiVersionSet() + WithApiVersionSet).
            // Calling it here (before MapDemoApiEndpoints) would capture an empty version
            // list and only show V1 in the Swagger UI dropdown.
            // It is called at the end of MapDemoApiEndpoints() instead.
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        // Controllers have been converted to Minimal API endpoints
        // app.MapControllers();
    }

    /// <summary>
    /// Maps all endpoints for the Demo API.
    /// </summary>
    public static void MapDemoApiEndpoints(this WebApplication app)
    {
        // ── Version set ──────────────────────────────────────────────────────────
        // All endpoint groups that should appear in the Swagger dropdown MUST share
        // the same IApiVersionSet (or at minimum reference it via WithApiVersionSet).
        ApiVersionSet apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .ReportApiVersions()
            .Build();

        // ── Re-usable version groups ──────────────────────────────────────────────
        // Assign each endpoint to exactly one group to control which Swagger
        // "definition" it appears in:
        //   v1Only   → visible under V1 only
        //   v2Only   → visible under V2 only
        //   allVersions → visible under both V1 and V2
        //
        // Using an empty prefix ("") means routes keep their original paths; the
        // group only adds version metadata to the endpoint.

        var v1Only = app.MapGroup("")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(1, 0);

        var v2Only = app.MapGroup("")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(2, 0);

        var allVersions = app.MapGroup("")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0));

        // ── Version-neutral ───────────────────────────────────────────────────────
        app.MapHealthCheckEndpoints(); // health checks are not versioned

        // ── V1-only endpoints ─────────────────────────────────────────────────────
        // Example: Barcode is kept at V1. A newer implementation can be added to
        // v2Only (e.g. a different barcode engine) without removing the V1 route.
        v1Only.MapBarcodeEndpoints();

        // ── V1-only demo: Dapper high-performance queries ─────────────────────────
        var dapperGroup = app.MapGroup("/api/demo/dapper-reports")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Dapper Reports Demo");
        dapperGroup.MapDapperReportEndpoints();

        // ── V2-only endpoints ─────────────────────────────────────────────────────
        // Example: S3 Storage & Email Notifications were introduced in v2.0.0.
        var storageGroup = app.MapGroup("/api/demo/storage")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(2, 0)
            .WithTags("Storage & Notifications Demo");
        storageGroup.MapStorageAndNotificationsEndpoints();

        // ── Available in both V1 and V2 ───────────────────────────────────────────
        allVersions.MapAllEndpoints();
        allVersions.MapAtsEndpoints();
        allVersions.MapDocumentoElectronicoEndpoints();
        allVersions.MapReportsEndpoints();
        allVersions.MapUsuarioEndpoints();
        allVersions.MapEncryptionEndpoints();
        allVersions.MapLookupEndpoints();
        allVersions.MapConfigurationTestEndpoints();
        allVersions.MapPrintEndpoints();
        allVersions.MapBusinessExceptionTestEndpoints();
        allVersions.MapExceptionTestEndpoints();
        allVersions.MapOrderEndpoints();
        allVersions.MapSalesAnalyticsEndpoints();

        // ── Swagger UI ────────────────────────────────────────────────────────────
        // Called LAST so app.DescribeApiVersions() (inside the library) reads the
        // fully-populated endpoint data sources and shows all registered versions
        // in the dropdown (V1, V2, …).
        if (app.Environment.IsDevelopment())
        {
            app.UseApiVersioningAndDocumentation();
        }
    }
}
