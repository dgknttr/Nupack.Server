using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Storage;

public sealed class PackageStorageWarmupHostedService : IHostedService
{
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<PackageStorageWarmupHostedService> _logger;

    public PackageStorageWarmupHostedService(IPackageStorageService storageService, ILogger<PackageStorageWarmupHostedService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var packageCount = await _storageService.GetTotalPackageCountAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Package storage initialized with {PackageCount} packages.", packageCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
