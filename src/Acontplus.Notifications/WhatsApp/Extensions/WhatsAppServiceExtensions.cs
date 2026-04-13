using Acontplus.Notifications.WhatsApp.Abstractions;
using Acontplus.Notifications.WhatsApp.Models;
using Acontplus.Notifications.WhatsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Acontplus.Notifications.WhatsApp.Extensions;

/// <summary>
/// Extension methods for registering the WhatsApp Cloud API notification service.
/// </summary>
public static class WhatsAppServiceExtensions
{
    /// <summary>
    /// Adds <see cref="IWhatsAppService"/> to the DI container, binding configuration
    /// from the <c>"WhatsApp"</c> section of <paramref name="configuration"/>.
    /// </summary>
    /// <remarks>
    /// Minimal appsettings.json:
    /// <code>
    /// {
    ///   "WhatsApp": {
    ///     "PhoneNumberId": "1234567890",
    ///     "AccessToken": "EAAxxxx...",
    ///     "ApiVersion": "v23.0",
    ///     "TimeoutSeconds": 30,
    ///     "DefaultCountryCode": "593"
    ///   }
    /// }
    /// </code>
    ///
    /// Multi-tenant appsettings.json:
    /// <code>
    /// {
    ///   "WhatsApp": {
    ///     "Accounts": {
    ///       "company-a": { "PhoneNumberId": "...", "AccessToken": "..." },
    ///       "company-b": { "PhoneNumberId": "...", "AccessToken": "..." }
    ///     }
    ///   }
    /// }
    /// </code>
    /// Then pass <c>Credentials = WhatsAppCredentials.FromAccount("company-a")</c> in your request.
    /// </remarks>
    public static IServiceCollection AddWhatsAppService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WhatsAppOptions>(
            configuration.GetSection(WhatsAppOptions.SectionName));

        return services.RegisterWhatsAppCore();
    }

    /// <summary>
    /// Adds <see cref="IWhatsAppService"/> to the DI container using an inline configuration action.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddWhatsAppService(opts =>
    /// {
    ///     opts.PhoneNumberId = "1234567890";
    ///     opts.AccessToken   = "EAAxxxx...";
    ///     opts.ApiVersion    = "v23.0";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddWhatsAppService(
        this IServiceCollection services,
        Action<WhatsAppOptions> configure)
    {
        services.Configure(configure);
        return services.RegisterWhatsAppCore();
    }

    // -------------------------------------------------------------------------

    private static IServiceCollection RegisterWhatsAppCore(this IServiceCollection services)
    {
        services
            .AddHttpClient(WhatsAppService.HttpClientName)
            .AddStandardResilienceHandler(opts =>
            {
                // Retry up to 3 times with exponential back-off.
                // Meta recommends waiting before retrying on 429 / 5xx responses.
                opts.Retry.MaxRetryAttempts = 3;
                opts.Retry.Delay = TimeSpan.FromMilliseconds(600);

                // Per-attempt timeout (before the retry kicks in).
                opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

                // Total timeout covering all attempts including retries.
                opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            });

        services.AddSingleton<IWhatsAppService, WhatsAppService>();

        return services;
    }
}
