using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationPlatform.Processing.Replay;

public static class ReplayServiceExtensions
{
    public static IServiceCollection AddMessageReplay(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ReplayOptions>(configuration.GetSection("Replay"));
        services.AddSingleton<IMessageReplayStore, InMemoryMessageReplayStore>();
        services.AddScoped<IMessageReplayer, MessageReplayer>();

        return services;
    }
}
