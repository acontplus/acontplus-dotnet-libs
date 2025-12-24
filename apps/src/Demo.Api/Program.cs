

// 1. Optional: Create a bootstrap logger for early startup issues
//    This captures logs from WebApplication.CreateBuilder() itself.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Get environment name once for clarity and re-use
    var environment = builder.Environment.EnvironmentName;

    // 2. Configure Serilog for the web host using the new extension method
    builder.Host.UseSerilog((hostContext, services, loggerConfiguration) =>
    {
        // Call the new method to apply your advanced logging settings to the LoggerConfiguration
        loggerConfiguration.ConfigureAdvancedLogger(hostContext.Configuration, environment);

        // This part tells Serilog to load its configuration from appsettings.json
        // and resolve any services (e.g., custom enrichers requiring DI)
        loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);
        loggerConfiguration.ReadFrom.Services(services);
    });

    // 3. Register your LoggingOptions class into the DI container
    //    This is where builder.Services (an IServiceCollection) is available.
    builder.Services.AddAdvancedLoggingOptions(builder.Configuration);

    // Configure all services using organized extension methods
    builder.Services
        .AddAllDemoServices(builder.Configuration)
        .AddDatabaseServices(builder.Configuration)
        .AddBusinessServices();

    builder.Services.AddApiVersioningAndDocumentation();
    var app = builder.Build();

    // Configure middleware and endpoints using organized extension methods
    app.ConfigureDemoApiMiddleware();
    app.MapDemoApiEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    // Catch any critical startup errors
    Log.Fatal(ex, "API host terminated unexpectedly.");
}
finally
{
    // Ensure all buffered logs are flushed on application shutdown
    Log.CloseAndFlush();
}

