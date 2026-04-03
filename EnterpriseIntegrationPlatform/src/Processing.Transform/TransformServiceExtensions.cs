using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Extension methods for registering Processing.Transform services into the DI container.
/// </summary>
public static class TransformServiceExtensions
{
    /// <summary>
    /// Registers a <see cref="TransformPipeline"/> and binds <see cref="TransformOptions"/>
    /// from the <c>TransformPipeline</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Transform steps must be registered separately as <see cref="ITransformStep"/>
    /// singletons before calling this method. The pipeline resolves all registered steps
    /// via <c>IEnumerable&lt;ITransformStep&gt;</c> and executes them in registration order.
    /// </remarks>
    public static IServiceCollection AddTransformPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TransformOptions>(configuration.GetSection("TransformPipeline"));
        services.AddSingleton<ITransformPipeline, TransformPipeline>();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="JsonToXmlStep"/> as a singleton <see cref="ITransformStep"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="rootElementName">
    /// XML root element name. Defaults to <c>Root</c>.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddJsonToXmlStep(
        this IServiceCollection services,
        string rootElementName = "Root")
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITransformStep>(new JsonToXmlStep(rootElementName));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="XmlToJsonStep"/> as a singleton <see cref="ITransformStep"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddXmlToJsonStep(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITransformStep>(new XmlToJsonStep());
        return services;
    }

    /// <summary>
    /// Registers a <see cref="RegexReplaceStep"/> as a singleton <see cref="ITransformStep"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="replacement">The replacement string.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRegexReplaceStep(
        this IServiceCollection services,
        string pattern,
        string replacement)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITransformStep>(new RegexReplaceStep(pattern, replacement));
        return services;
    }

    /// <summary>
    /// Registers a <see cref="JsonPathFilterStep"/> as a singleton <see cref="ITransformStep"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="paths">Dot-separated property paths to retain.</param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddJsonPathFilterStep(
        this IServiceCollection services,
        params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITransformStep>(new JsonPathFilterStep(paths));
        return services;
    }
}
