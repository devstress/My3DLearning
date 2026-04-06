// ============================================================================
// AspireIntegrationTestHost – DI-based test host for E2E integration testing
// ============================================================================
// Wires real EIP components with MockEndpoints using the same
// HostApplicationBuilder pattern as .NET Aspire. Provides service resolution
// for end-to-end integration tests.
// ============================================================================

using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Infrastructure;

/// <summary>
/// Aspire-style integration test host that wires real EIP components
/// with MockEndpoints via dependency injection.
/// </summary>
public sealed class AspireIntegrationTestHost : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly Dictionary<string, MockEndpoint> _endpoints;

    internal AspireIntegrationTestHost(IHost host, Dictionary<string, MockEndpoint> endpoints)
    {
        _host = host;
        _endpoints = endpoints;
    }

    public IServiceProvider Services => _host.Services;

    public T GetService<T>() where T : notnull =>
        _host.Services.GetRequiredService<T>();

    public MockEndpoint GetEndpoint(string name) => _endpoints[name];

    public IReadOnlyDictionary<string, MockEndpoint> Endpoints => _endpoints;

    public static Builder CreateBuilder() => new();

    public ValueTask DisposeAsync()
    {
        _host.Dispose();
        return ValueTask.CompletedTask;
    }

    public sealed class Builder
    {
        private readonly HostApplicationBuilder _inner;
        private readonly Dictionary<string, MockEndpoint> _endpoints = new();

        public Builder()
        {
            _inner = Host.CreateApplicationBuilder([]);
            _inner.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            _inner.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        }

        /// <summary>
        /// Adds a named MockEndpoint for testing.
        /// </summary>
        public MockEndpoint AddMockEndpoint(string name)
        {
            var ep = new MockEndpoint(name);
            _endpoints[name] = ep;
            return ep;
        }

        /// <summary>
        /// Registers a MockEndpoint as the default IMessageBrokerProducer.
        /// </summary>
        public Builder UseProducer(MockEndpoint endpoint)
        {
            _inner.Services.AddSingleton<IMessageBrokerProducer>(endpoint);
            return this;
        }

        /// <summary>
        /// Registers a MockEndpoint as the default IMessageBrokerConsumer.
        /// </summary>
        public Builder UseConsumer(MockEndpoint endpoint)
        {
            _inner.Services.AddSingleton<IMessageBrokerConsumer>(endpoint);
            return this;
        }

        public Builder ConfigureServices(Action<IServiceCollection> configure)
        {
            configure(_inner.Services);
            return this;
        }

        public Builder AddSingleton<TService>(TService instance) where TService : class
        {
            _inner.Services.AddSingleton(instance);
            return this;
        }

        public Builder Configure<TOptions>(Action<TOptions> configure) where TOptions : class
        {
            _inner.Services.Configure(configure);
            return this;
        }

        public AspireIntegrationTestHost Build()
        {
            var host = _inner.Build();
            return new AspireIntegrationTestHost(host, _endpoints);
        }
    }
}
