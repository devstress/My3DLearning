using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// DI registration extensions for the Request-Reply pattern services.
/// </summary>
public static class RequestReplyServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IRequestReplyCorrelator{TRequest,TResponse}"/> and
    /// <see cref="RequestReplyOptions"/> from configuration.
    /// </summary>
    public static IServiceCollection AddRequestReplyCorrelator<TRequest, TResponse>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RequestReplyOptions>(configuration.GetSection("RequestReply"));
        services.AddScoped<IRequestReplyCorrelator<TRequest, TResponse>, RequestReplyCorrelator<TRequest, TResponse>>();

        return services;
    }
}
