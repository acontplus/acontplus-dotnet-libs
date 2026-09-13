using Acontplus.Core.Abstractions.Messaging;
using Acontplus.Core.Domain.Common.Events;
using Acontplus.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acontplus.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring the in-memory event bus in the dependency injection container.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Adds the in-memory event bus as a singleton service to the service collection.
    /// Registers IEventBus, IEventPublisher, and IEventSubscriber interfaces.
    /// </summary>
    /// <param name="services">The service collection to add the event bus to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services) =>
        services.AddInMemoryEventBus(_ => { });

    /// <summary>
    /// Adds the in-memory event bus as a singleton service with configuration options.
    /// Registers IEventBus, IEventPublisher, and IEventSubscriber interfaces.
    /// </summary>
    /// <param name="services">The service collection to add the event bus to.</param>
    /// <param name="configureOptions">Action to configure event bus options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInMemoryEventBus(
        this IServiceCollection services,
        Action<EventBusOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options
        services.Configure(configureOptions);

        // Register the InMemoryEventBus as a singleton
        services.TryAddSingleton<InMemoryEventBus>();

        // Register all interface projections pointing to the same singleton instance
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.TryAddSingleton<IEventPublisher>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.TryAddSingleton<IEventSubscriber>(sp => sp.GetRequiredService<InMemoryEventBus>());

        return services;
    }

    /// <summary>
    /// Adds the domain event dispatcher for synchronous domain event handling.
    /// Use this for transactional domain events that must run in the same Unit of Work.
    /// </summary>
    /// <param name="services">The service collection to add the domain event dispatcher to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the domain event dispatcher as scoped (same lifetime as DbContext/UoW)
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }

    /// <summary>
    /// Registers a domain event handler for a specific domain event type.
    /// Handlers are executed synchronously in the same transaction when events are dispatched.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register as scoped to match DbContext/UoW lifetime
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();

        return services;
    }
}
