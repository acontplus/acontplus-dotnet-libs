using Acontplus.Utilities.Mapping;
using Acontplus.Utilities.Mapping.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.Utilities.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Acontplus.Utilities services.
/// </summary>
public static partial class UtilitiesServiceExtensions
{
    /// <summary>
    /// Registers the compiled object mapper as a singleton in the service collection.
    /// Configures the <see cref="MapperConfiguration"/> with the provided profiles, compiles
    /// all mapping delegates at startup, and registers the resulting <see cref="IObjectMapper"/>
    /// as a singleton.
    /// </summary>
    /// <param name="services">The service collection to register the mapper into.</param>
    /// <param name="profiles">
    /// Zero or more <see cref="MappingProfile"/> instances to register. When no profiles are
    /// provided, convention-based mappings are still available on demand.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IObjectMapper"/> has already been registered in the service collection.
    /// </exception>
    public static IServiceCollection AddObjectMapper(
        this IServiceCollection services,
        params MappingProfile[] profiles)
    {
        if (services.Any(d => d.ServiceType == typeof(IObjectMapper)))
        {
            throw new InvalidOperationException(
                "IObjectMapper has already been registered. Only one call to AddObjectMapper is allowed.");
        }

        services.AddSingleton<IObjectMapper>(_ =>
        {
            var configuration = new MapperConfiguration();

            configuration.CompileDelegate = (pair, config, registry) =>
            {
                var expression = ExpressionBuilder.BuildMappingExpression(
                    pair, config, registry, new HashSet<TypePair>());
                return DelegateCompiler.Compile(expression);
            };

            foreach (var profile in profiles)
            {
                configuration.AddProfile(profile);
            }

            var mapperRegistry = configuration.Build();
            return new CompiledObjectMapper(mapperRegistry);
        });

        return services;
    }
}
