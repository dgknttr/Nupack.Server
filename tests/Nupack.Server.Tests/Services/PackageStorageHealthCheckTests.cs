using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Nupack.Server.Api.Services;
using Nupack.Server.Storage.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class PackageStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenStorageProbeSucceeds_ReturnsHealthy()
    {
        var storage = new Mock<IPackageStorageService>();
        var healthCheck = new PackageStorageHealthCheck(storage.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageProbeFails_ReturnsSafeUnhealthyResult()
    {
        var storage = new Mock<IPackageStorageService>();
        storage.Setup(value => value.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AccessKey=super-secret"));
        var healthCheck = new PackageStorageHealthCheck(storage.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().NotContain("super-secret");
        result.Exception.Should().BeNull();
    }
}
