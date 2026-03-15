using System.Text;

using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Processing.Transform;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Claim Check pattern.
/// Stores large payloads externally and passes a claim token instead.
/// BizTalk equivalent: Large message handling via streaming or external storage.
/// EIP: Claim Check (p. 346)
/// </summary>
public class ClaimCheckTests
{
    [Fact]
    public async Task CheckIn_StoresPayload_ReturnsToken()
    {
        var store = new InMemoryClaimCheckStore();

        var largePayload = Encoding.UTF8.GetBytes(new string('X', 10_000));
        var token = await store.CheckInAsync(largePayload);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckOut_RetrievesOriginalPayload()
    {
        var store = new InMemoryClaimCheckStore();

        var original = Encoding.UTF8.GetBytes("large document content here");
        var token = await store.CheckInAsync(original);

        var retrieved = await store.CheckOutAsync(token);

        retrieved.Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task CheckOut_InvalidToken_Throws()
    {
        var store = new InMemoryClaimCheckStore();

        var act = () => store.CheckOutAsync("invalid-token");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
