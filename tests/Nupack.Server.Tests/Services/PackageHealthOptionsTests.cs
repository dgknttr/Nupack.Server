using FluentAssertions;
using Nupack.Server.Api.Models;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class PackageHealthOptionsTests
{
    [Fact]
    public void ResolveReadinessTimeout_WithPositiveSeconds_UsesConfiguredValue()
    {
        PackageHealthOptions.ResolveReadinessTimeout("12").Should().Be(TimeSpan.FromSeconds(12));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("301")]
    public void ResolveReadinessTimeout_WithInvalidValue_UsesSafeDefault(string? value)
    {
        PackageHealthOptions.ResolveReadinessTimeout(value)
            .Should().Be(TimeSpan.FromSeconds(PackageHealthOptions.DefaultReadinessTimeoutSeconds));
    }
}
