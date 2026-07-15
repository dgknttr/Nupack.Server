using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
        var healthCheck = new PackageStorageHealthCheck(storage.Object, Mock.Of<ILogger<PackageStorageHealthCheck>>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageProbeFails_ReturnsSafeUnhealthyResult()
    {
        var storage = new Mock<IPackageStorageService>();
        storage.Setup(value => value.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AccessKey=super-secret"));
        var logger = new Mock<ILogger<PackageStorageHealthCheck>>();
        var healthCheck = new PackageStorageHealthCheck(storage.Object, logger.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().NotContain("super-secret");
        result.Exception.Should().BeNull();
        var warning = logger.Invocations.Should().ContainSingle(invocation => invocation.Method.Name == nameof(ILogger.Log)).Subject;
        warning.Arguments[0].Should().Be(LogLevel.Warning);
        warning.Arguments[2].ToString().Should().Contain(nameof(InvalidOperationException));
        warning.Arguments[2].ToString().Should().NotContain("super-secret");
        warning.Arguments[3].Should().BeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var storage = new Mock<IPackageStorageService>();
        storage.Setup(value => value.CheckHealthAsync(cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var healthCheck = new PackageStorageHealthCheck(storage.Object, Mock.Of<ILogger<PackageStorageHealthCheck>>());

        var action = () => healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
