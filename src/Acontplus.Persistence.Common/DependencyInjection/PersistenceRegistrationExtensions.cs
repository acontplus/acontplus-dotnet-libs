using Acontplus.Persistence.Common.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acontplus.Persistence.Common.DependencyInjection;

/// <summary>
/// Provides extension methods for registering common persistence services, such as IAdoRepository and IUnitOfWork.
/// </summary>
public static class PersistenceRegistrationExtensions
{
    /// <summary>
    /// Registers the ADO repository, UnitOfWork, and DbContext in the service collection.
    /// Supports both keyed and standard scoped registrations.
    /// </summary>
    /// <typeparam name="TContext">The DbContext implementation type.</typeparam>
    /// <typeparam name="TAdoRepository">The IAdoRepository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">Optional service key for keyed registrations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterPersistenceCore<TContext, TAdoRepository>(
        this IServiceCollection services,
        object? serviceKey = null)
        where TContext : DbContext
        where TAdoRepository : class, IAdoRepository
    {
        ArgumentNullException.ThrowIfNull(services);

        if (serviceKey is not null)
        {
            services.TryAddKeyedScoped<IAdoRepository, TAdoRepository>(serviceKey);
            services.TryAddKeyedScoped<IUnitOfWork, UnitOfWork<TContext>>(serviceKey);
            services.TryAddKeyedScoped<DbContext>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<TContext>(key));
        }
        else
        {
            services.TryAddScoped<IAdoRepository, TAdoRepository>();
            services.TryAddScoped<IUnitOfWork, UnitOfWork<TContext>>();
            services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        }

        return services;
    }
}
