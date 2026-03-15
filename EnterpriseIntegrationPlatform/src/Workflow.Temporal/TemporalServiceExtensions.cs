using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Temporalio.Extensions.Hosting;

using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Activities;
using EnterpriseIntegrationPlatform.Workflow.Temporal.Workflows;

namespace EnterpriseIntegrationPlatform.Workflow.Temporal;

/// <summary>
/// Extension methods for registering Temporal workflow infrastructure
/// in the dependency injection container.
/// </summary>
public static class TemporalServiceExtensions
{
    /// <summary>
    /// Registers Temporal worker, workflow definitions, and activity services.
    /// Reads configuration from the <c>Temporal</c> section.
    /// </summary>
    public static IServiceCollection AddTemporalWorkflows(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new TemporalOptions();
        configuration.GetSection(TemporalOptions.SectionName).Bind(options);

        // Register activity business-logic services
        services.AddSingleton<IMessageValidationService, DefaultMessageValidationService>();
        services.AddSingleton<IMessageLoggingService, DefaultMessageLoggingService>();

        // Register Temporal hosted worker with workflows and scoped activities
        services
            .AddHostedTemporalWorker(options.TaskQueue)
            .ConfigureOptions(workerOpts =>
            {
                workerOpts.ClientOptions = new()
                {
                    TargetHost = options.ServerAddress,
                    Namespace = options.Namespace,
                };
            })
            .AddWorkflow<ProcessIntegrationMessageWorkflow>()
            .AddScopedActivities<IntegrationActivities>();

        return services;
    }
}
