using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Api.Services;

public sealed class PackageStorageHealthCheck : IHealthCheck
{
    private const string UnavailableDescription = "Package storage is unavailable.";
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<PackageStorageHealthCheck> _logger;

    public PackageStorageHealthCheck(
        IPackageStorageService storageService,
        ILogger<PackageStorageHealthCheck> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageService.CheckHealthAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Package storage readiness probe failed with {FailureType}.",
                exception.GetType().Name);
            return HealthCheckResult.Unhealthy(UnavailableDescription);
        }
    }
}
