using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Api.Services;

public sealed class PackageStorageHealthCheck : IHealthCheck
{
    private const string UnavailableDescription = "Package storage is unavailable.";
    private readonly IPackageStorageService _storageService;

    public PackageStorageHealthCheck(IPackageStorageService storageService)
    {
        _storageService = storageService;
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
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy(UnavailableDescription);
        }
    }
}
