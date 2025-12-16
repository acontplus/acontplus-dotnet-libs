using Acontplus.Persistence.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acontplus.Persistence.SqlServer.DependencyInjection;

public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    ///     Registers a SQL Server DbContext and its corresponding UnitOfWork implementation,
    ///     optionally with a service key using .NET 8+ keyed DI.
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

        if (serviceKey is not null)
        {
            services.TryAddKeyedScoped<IUnitOfWork, UnitOfWork<TContext>>(serviceKey);
            services.TryAddKeyedScoped<DbContext>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<TContext>(key));
        }
        else
        {
            services.TryAddScoped<IUnitOfWork, UnitOfWork<TContext>>();
            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        }

        // Register PersistenceResilienceOptions with default values
        // To configure from appsettings.json, add this in your Startup/Program.cs:
        // services.Configure<PersistenceResilienceOptions>(configuration.GetSection("Persistence:Resilience"));
        services.TryAddSingleton<Microsoft.Extensions.Options.IOptions<PersistenceResilienceOptions>>(
            sp => Microsoft.Extensions.Options.Options.Create(new PersistenceResilienceOptions()));

        return services;
    }
}
