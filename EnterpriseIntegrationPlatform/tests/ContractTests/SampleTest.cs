using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Contract;

public class SampleTest
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
