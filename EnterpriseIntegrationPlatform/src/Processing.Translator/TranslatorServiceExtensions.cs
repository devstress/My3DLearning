using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Extension methods for registering Message Translator services into the DI container.
/// </summary>
public static class TranslatorServiceExtensions
{
    /// <summary>
    /// Registers a <see cref="MessageTranslator{TIn,TOut}"/> backed by a delegate-based
    /// payload transform.
    /// </summary>
    /// <typeparam name="TIn">Source payload type.</typeparam>
    /// <typeparam name="TOut">Target payload type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>MessageTranslator</c> section is bound to
    /// <see cref="TranslatorOptions"/>.
    /// </param>
    /// <param name="transformFunc">
    /// Delegate that converts a <typeparamref name="TIn"/> payload to
    /// <typeparamref name="TOut"/>.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddMessageTranslator<TIn, TOut>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<TIn, TOut> transformFunc)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(transformFunc);

        services.Configure<TranslatorOptions>(configuration.GetSection("MessageTranslator"));
        services.AddSingleton<IPayloadTransform<TIn, TOut>>(
            new FuncPayloadTransform<TIn, TOut>(transformFunc));
        services.AddSingleton<IMessageTranslator<TIn, TOut>, MessageTranslator<TIn, TOut>>();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MessageTranslator{JsonElement,JsonElement}"/> backed by
    /// <see cref="JsonFieldMappingTransform"/>, which maps fields from the source JSON
    /// document to the target JSON document using the <see cref="FieldMapping"/> list
    /// defined in the <c>MessageTranslator:FieldMappings</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration; the <c>MessageTranslator</c> section is bound to
    /// <see cref="TranslatorOptions"/>.
    /// </param>
    /// <returns>The updated <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Requires an <see cref="Ingestion.IMessageBrokerProducer"/> to already be registered
    /// (e.g. via <c>AddNatsJetStreamBroker</c>).
    /// </remarks>
    public static IServiceCollection AddJsonMessageTranslator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TranslatorOptions>(configuration.GetSection("MessageTranslator"));
        services.AddSingleton<IPayloadTransform<JsonElement, JsonElement>, JsonFieldMappingTransform>();
        services.AddSingleton<IMessageTranslator<JsonElement, JsonElement>,
            MessageTranslator<JsonElement, JsonElement>>();
        return services;
    }
}
