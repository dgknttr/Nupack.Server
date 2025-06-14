using Nupack.Server.Web.Models;

namespace Nupack.Server.Web.Services;

public interface INuGetApiService
{
    Task<ServiceIndex?> GetServiceIndexAsync();
    Task<SearchResponse?> SearchPackagesAsync(string? query = null, int skip = 0, int take = 20, bool includePrerelease = false);
    Task<PackageVersionsIndex?> GetPackageVersionsAsync(string packageId);
    Task<RegistrationIndex?> GetPackageRegistrationAsync(string packageId);
    Task<RegistrationLeaf?> GetPackageVersionRegistrationAsync(string packageId, string version);
    Task<HealthStatus?> GetHealthStatusAsync();
}
