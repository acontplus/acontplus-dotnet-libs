using Acontplus.Core.Domain.Common.Events;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.S3Application.Extensions;
using Demo.Api.Endpoints.Business.Analytics;
using Demo.Api.Endpoints.Demo;
using Demo.Api.Endpoints.Infrastructure;
using Demo.Application.Services;
using Demo.Infrastructure.EventHandlers;
using Demo.Infrastructure.Persistence;
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
            "Acontplus.Core.Security.Services",
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
            app.UseApiVersioningAndDocumentation();
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
        // Map health checks using infrastructure extension
        app.MapHealthCheckEndpoints();

        // Map all organized endpoints
        app.MapAllEndpoints();

        // Map specific business endpoints
        app.MapAtsEndpoints();
        app.MapDocumentoElectronicoEndpoints();
        app.MapReportsEndpoints();
        app.MapUsuarioEndpoints();

        // Map core endpoints
        app.MapEncryptionEndpoints();
        app.MapLookupEndpoints();

        // Map infrastructure endpoints
        app.MapBarcodeEndpoints();
        app.MapConfigurationTestEndpoints();
        app.MapPrintEndpoints();

        // ========================================
        // DEMO: S3 STORAGE & EMAIL NOTIFICATIONS
        // ========================================
        // Demonstrates v2.0.0 S3 and v1.5.0 Notifications features
        var storageGroup = app.MapGroup("/api/demo/storage")
            .WithTags("Storage & Notifications Demo");
        storageGroup.MapStorageAndNotificationsEndpoints();

        // ========================================
        // DEMO: DAPPER REPOSITORY (v2.2.0)
        // ========================================
        // High-performance read operations using Dapper
        var dapperGroup = app.MapGroup("/api/demo/dapper-reports")
            .WithTags("Dapper Reports Demo");
        dapperGroup.MapDapperReportEndpoints();

        // Map demo endpoints
        app.MapBusinessExceptionTestEndpoints();
        app.MapExceptionTestEndpoints();

        // Map CQRS + Event Bus demo endpoints
        app.MapOrderEndpoints();

        // Map Analytics demo endpoints
        app.MapSalesAnalyticsEndpoints();
    }
}
