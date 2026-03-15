using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Extension methods for registering Message Translator services in the DI container.
/// </summary>
public static class TransformServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IMessageTranslator"/> singleton and the built-in payload
    /// converters as transient services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageTranslator(this IServiceCollection services)
    {
        services.AddSingleton<IMessageTranslator, MessageTranslator>();

        services.AddTransient<Converters.JsonToXmlConverter>();
        services.AddTransient<Converters.XmlToJsonConverter>();
        services.AddTransient<Converters.CsvToJsonConverter>();

        return services;
    }
}
