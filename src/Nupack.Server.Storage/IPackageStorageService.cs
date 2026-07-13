using Nupack.Server.Storage.Models;

namespace Nupack.Server.Storage.Services;

public interface IPackageStorageService
{
    Task CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<PackageMetadata> StorePackageAsync(PackageUploadContent package, CancellationToken cancellationToken = default);
    Task<IEnumerable<PackageMetadata>> GetPackagesAsync(string? searchQuery = null, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<PackageMetadata?> GetPackageAsync(string id, string version, CancellationToken cancellationToken = default);
    Task<Stream?> GetPackageStreamAsync(string id, string version, CancellationToken cancellationToken = default);
    Task<bool> DeletePackageAsync(string id, string version, CancellationToken cancellationToken = default);
    Task<int> GetTotalPackageCountAsync(string? searchQuery = null, CancellationToken cancellationToken = default);
}
