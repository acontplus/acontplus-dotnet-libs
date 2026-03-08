using Acontplus.Barcode.Interfaces;
using Acontplus.Barcode.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.Barcode.Extensions;

/// <summary>
/// Extension methods for registering barcode services in the dependency injection container.
/// </summary>
public static class BarcodeServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBarcodeService"/> as a singleton in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBarcode(this IServiceCollection services)
    {
        services.AddSingleton<IBarcodeService, BarcodeService>();
        return services;
    }
}
