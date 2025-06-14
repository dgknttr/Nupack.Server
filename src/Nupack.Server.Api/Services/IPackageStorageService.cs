using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public interface IPackageStorageService
{
    Task<PackageMetadata> StorePackageAsync(IFormFile packageFile);
    Task<IEnumerable<PackageMetadata>> GetPackagesAsync(string? searchQuery = null, int skip = 0, int take = 20);
    Task<PackageMetadata?> GetPackageAsync(string id, string version);
    Task<Stream?> GetPackageStreamAsync(string id, string version);
    Task<bool> DeletePackageAsync(string id, string version);
    Task<int> GetTotalPackageCountAsync(string? searchQuery = null);
}
