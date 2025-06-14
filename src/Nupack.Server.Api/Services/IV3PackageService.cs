using Nupack.Server.Api.Models.V3;
using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public interface IV3PackageService
{
    // Service Index
    ServiceIndex GetServiceIndex(string baseUrl);

    // Search Service
    Task<SearchResponse> SearchPackagesAsync(string? query = null, int skip = 0, int take = 20,
        bool includePrerelease = true, string? semVerLevel = null, string? baseUrl = null);

    // Package Base Address (Flat Container)
    Task<PackageVersionsIndex?> GetPackageVersionsAsync(string packageId);
    Task<Stream?> GetPackageContentAsync(string packageId, string version);
    Task<Stream?> GetPackageManifestAsync(string packageId, string version);

    // Registration (baseUrl is required and provided by HTTP endpoints via IBaseUrlResolver)
    Task<RegistrationIndex?> GetRegistrationIndexAsync(string packageId, string baseUrl);
    Task<RegistrationPage?> GetRegistrationPageAsync(string packageId, string lower, string upper, string baseUrl);
    Task<RegistrationLeaf?> GetRegistrationLeafAsync(string packageId, string version, string baseUrl);

    // Package Management
    Task<bool> PackageExistsAsync(string packageId, string version);
    Task<IEnumerable<string>> GetAllPackageIdsAsync();
    Task<IEnumerable<string>> GetPackageVersionsListAsync(string packageId);
}
