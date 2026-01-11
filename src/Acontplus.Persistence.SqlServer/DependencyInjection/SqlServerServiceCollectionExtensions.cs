using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.SqlServer.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acontplus.Persistence.SqlServer.DependencyInjection;

public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Ensures PersistenceResilienceOptions is registered with default values.
    /// Called internally by persistence registration methods.
    /// To configure from appsettings.json, add this in your Startup/Program.cs:
    /// services.Configure&lt;PersistenceResilienceOptions&gt;(configuration.GetSection("Persistence:Resilience"));
    /// </summary>
    private static void EnsureResilienceOptionsRegistered(IServiceCollection services)
    {
        services.TryAddSingleton<Microsoft.Extensions.Options.IOptions<PersistenceResilienceOptions>>(
            sp => Microsoft.Extensions.Options.Options.Create(new PersistenceResilienceOptions()));
    }

    /// <summary>
    ///     Registers a SQL Server DbContext and its corresponding UnitOfWork implementation,
    ///     optionally with a service key using .NET 8+ keyed DI.
    ///     Also registers IAdoRepository which is required by UnitOfWork.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to register.</typeparam>
    /// <param name="services">The IServiceCollection.</param>
    /// <param name="sqlServerOptions">The SQL Server-specific options for DbContext.</param>
    /// <param name="serviceKey">Optional key to register the services with (for keyed DI).</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddSqlServerPersistence<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> sqlServerOptions,
        object? serviceKey = null)
        where TContext : DbContext
    {
        services.AddDbContextPool<TContext>(sqlServerOptions, 128);

        EnsureResilienceOptionsRegistered(services);

        // Register IAdoRepository which is required by UnitOfWork
        if (serviceKey is not null)
        {
            services.TryAddKeyedScoped<IAdoRepository, AdoRepository>(serviceKey);
            services.TryAddKeyedScoped<IUnitOfWork, UnitOfWork<TContext>>(serviceKey);
            services.TryAddKeyedScoped<DbContext>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<TContext>(key));
        }
        else
        {
            services.TryAddScoped<IAdoRepository, AdoRepository>();
            services.TryAddScoped<IUnitOfWork, UnitOfWork<TContext>>();
            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        }

        return services;
    }

    /// <summary>
    /// Registers the Dapper repository for SQL Server with resilience policies.
    /// Can be used independently or alongside Entity Framework Core.
    /// </summary>
    /// <param name="services">The IServiceCollection.</param>
    /// <param name="serviceKey">Optional key to register the service with (for keyed DI).</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddSqlServerDapperRepository(
        this IServiceCollection services,
        object? serviceKey = null)
    {
        EnsureResilienceOptionsRegistered(services);

        if (serviceKey is not null)
        {
            services.TryAddKeyedScoped<IDapperRepository, DapperRepository>(serviceKey);
        }
        else
        {
            services.TryAddScoped<IDapperRepository, DapperRepository>();
        }

        return services;
    }

    /// <summary>
    /// Registers the ADO.NET repository for SQL Server with resilience policies.
    /// Can be used independently or alongside Entity Framework Core.
    /// </summary>
    /// <param name="services">The IServiceCollection.</param>
    /// <param name="serviceKey">Optional key to register the service with (for keyed DI).</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddSqlServerAdoRepository(
        this IServiceCollection services,
        object? serviceKey = null)
    {
        EnsureResilienceOptionsRegistered(services);

        if (serviceKey is not null)
        {
            services.TryAddKeyedScoped<IAdoRepository, AdoRepository>(serviceKey);
        }
        else
        {
            services.TryAddScoped<IAdoRepository, AdoRepository>();
        }

        return services;
    }
}
