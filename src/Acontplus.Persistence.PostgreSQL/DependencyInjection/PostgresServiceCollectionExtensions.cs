using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.DependencyInjection;
using Acontplus.Persistence.PostgreSQL.Interceptors;
using Acontplus.Persistence.PostgreSQL.Repositories;

namespace Acontplus.Persistence.PostgreSQL.DependencyInjection;

/// <summary>
/// Extension methods for registering PostgreSQL persistence services with the DI container.
/// </summary>
public static class PostgresServiceCollectionExtensions
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
  /// Registers a PostgreSQL DbContext and its corresponding UnitOfWork implementation,
  /// optionally with a service key using .NET 8+ keyed DI.
  /// Also registers IAdoRepository which is required by UnitOfWork.
  /// </summary>
  /// <typeparam name="TContext">The DbContext type to register.</typeparam>
  /// <param name="services">The IServiceCollection.</param>
  /// <param name="postgresOptions">The PostgreSQL-specific options for DbContext.</param>
  /// <param name="serviceKey">Optional key to register the services with (for keyed DI).</param>
  /// <returns>The updated IServiceCollection.</returns>
  public static IServiceCollection AddPostgresPersistence<TContext>(
      this IServiceCollection services,
      Action<DbContextOptionsBuilder> postgresOptions,
      object? serviceKey = null)
      where TContext : DbContext
  {
    services.TryAddSingleton<AuditSaveChangesInterceptor>();

    services.AddDbContextPool<TContext>((sp, options) =>
    {
      postgresOptions(options);
      options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
    }, poolSize: 128);

    EnsureResilienceOptionsRegistered(services);

    services.RegisterPersistenceCore<TContext, AdoRepository>(serviceKey);

    return services;
  }

  /// <summary>
  /// Registers the Dapper repository for PostgreSQL with resilience policies.
  /// Can be used independently or alongside Entity Framework Core.
  /// </summary>
  /// <param name="services">The IServiceCollection.</param>
  /// <param name="serviceKey">Optional key to register the service with (for keyed DI).</param>
  /// <returns>The updated IServiceCollection.</returns>
  public static IServiceCollection AddPostgresDapperRepository(
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
  /// Registers the ADO.NET repository for PostgreSQL with resilience policies.
  /// Can be used independently or alongside Entity Framework Core.
  /// </summary>
  /// <param name="services">The IServiceCollection.</param>
  /// <param name="serviceKey">Optional key to register the service with (for keyed DI).</param>
  /// <returns>The updated IServiceCollection.</returns>
  public static IServiceCollection AddPostgresAdoRepository(
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
