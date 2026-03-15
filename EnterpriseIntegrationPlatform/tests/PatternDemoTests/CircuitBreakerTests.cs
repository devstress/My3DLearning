using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Circuit Breaker pattern.
/// Prevents repeated calls to a failing service by opening the circuit
/// after a threshold of failures.
/// BizTalk equivalent: Adapter retry + service window patterns.
/// EIP: Circuit Breaker (related to Guaranteed Delivery, p. 122)
/// </summary>
public class CircuitBreakerTests
{
    [Fact]
    public async Task Closed_WhenHealthy()
    {
        var breaker = new CircuitBreaker(failureThreshold: 3);

        breaker.State.Should().Be(CircuitState.Closed);

        var result = await breaker.ExecuteAsync(async ct => "ok");

        result.Should().Be("ok");
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task Opens_AfterThresholdFailures()
    {
        var breaker = new CircuitBreaker(
            failureThreshold: 2,
            openDuration: TimeSpan.FromMinutes(5));

        // Two failures should open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await breaker.ExecuteAsync<string>(async ct =>
                    throw new TimeoutException("service down"));
            }
            catch (TimeoutException) { }
        }

        breaker.State.Should().Be(CircuitState.Open);

        // Subsequent calls should fail immediately
        var act = () => breaker.ExecuteAsync(async ct => "should not reach");
        await act.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task Resets_AfterSuccessfulCall()
    {
        var breaker = new CircuitBreaker(failureThreshold: 3);

        // One failure
        try
        {
            await breaker.ExecuteAsync<string>(async ct =>
                throw new Exception("transient"));
        }
        catch { }

        // Successful call resets failure count
        await breaker.ExecuteAsync(async ct => "recovered");

        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task Manual_Reset_ClosesCircuit()
    {
        var breaker = new CircuitBreaker(
            failureThreshold: 1,
            openDuration: TimeSpan.FromMinutes(5));

        try
        {
            await breaker.ExecuteAsync<string>(async ct =>
                throw new Exception("fail"));
        }
        catch { }

        breaker.State.Should().Be(CircuitState.Open);

        breaker.Reset();

        breaker.State.Should().Be(CircuitState.Closed);
    }
}
