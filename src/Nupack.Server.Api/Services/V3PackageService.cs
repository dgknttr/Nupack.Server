using Nupack.Server.Api.Models;
using Nupack.Server.Api.Models.V3;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Nupack.Server.Api.Services;

public class V3PackageService : IV3PackageService
{
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<V3PackageService> _logger;

    public V3PackageService(IPackageStorageService storageService, ILogger<V3PackageService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public ServiceIndex GetServiceIndex(string baseUrl)
    {
        return new ServiceIndex
        {
            Version = "3.0.0",
            Resources = new List<ServiceResource>
            {
                new()
                {
                    Id = $"{baseUrl}/v3-flatcontainer/",
                    Type = "PackageBaseAddress/3.0.0",
                    Comment = "Base URL of where NuGet packages are stored, in the format https://api.nuget.org/v3-flatcontainer/{{id-lower}}/{{version-lower}}/{{id-lower}}.{{version-lower}}.nupkg"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/search",
                    Type = "SearchQueryService",
                    Comment = "Query endpoint of NuGet Search service (primary)"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/search",
                    Type = "SearchQueryService/3.0.0-beta",
                    Comment = "Query endpoint of NuGet Search service (primary)"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/search",
                    Type = "SearchQueryService/3.0.0-rc",
                    Comment = "Query endpoint of NuGet Search service (primary)"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/registrations/",
                    Type = "RegistrationsBaseUrl",
                    Comment = "Base URL of Azure storage where NuGet package registration info is stored, in the format https://api.nuget.org/v3/registrations2/{{id-lower}}/index.json"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/registrations/",
                    Type = "RegistrationsBaseUrl/3.0.0-beta",
                    Comment = "Base URL of Azure storage where NuGet package registration info is stored, in the format https://api.nuget.org/v3/registrations2/{{id-lower}}/index.json"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/registrations/",
                    Type = "RegistrationsBaseUrl/3.0.0-rc",
                    Comment = "Base URL of Azure storage where NuGet package registration info is stored, in the format https://api.nuget.org/v3/registrations2/{{id-lower}}/index.json"
                }
            }
        };
    }

    public async Task<SearchResponse> SearchPackagesAsync(string? query = null, int skip = 0, int take = 20,
        bool includePrerelease = true, string? semVerLevel = null, string? baseUrl = null)
    {
        try
        {
            // Get all packages from storage
            var allPackages = await _storageService.GetPackagesAsync(query, 0, int.MaxValue);

            // Filter prerelease if needed
            if (!includePrerelease)
            {
                allPackages = allPackages.Where(p => !IsPrerelease(p.Version));
            }

            // baseUrl should be provided by HTTP endpoints via IBaseUrlResolver
            // This fallback is only for backward compatibility - prefer explicit baseUrl
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogWarning("SearchPackagesAsync called without baseUrl. This is deprecated - use IBaseUrlResolver in HTTP endpoints.");
                baseUrl = "http://localhost:5003"; // Minimal fallback
            }

            // Group by package ID and convert to search results
            var groupedPackages = allPackages
                .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => ConvertToSearchResult(g.Key, g.ToList(), baseUrl))
                .OrderByDescending(p => p.TotalDownloads)
                .ThenBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase);

            var totalHits = groupedPackages.Count();
            var pagedResults = groupedPackages.Skip(skip).Take(take).ToList();

            return new SearchResponse
            {
                TotalHits = totalHits,
                Data = pagedResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search packages with query: {Query}", query);
            return new SearchResponse { TotalHits = 0, Data = new List<SearchResult>() };
        }
    }

    public async Task<PackageVersionsIndex?> GetPackageVersionsAsync(string packageId)
    {
        try
        {
            var packages = await _storageService.GetPackagesAsync(packageId, 0, int.MaxValue);
            var versions = packages
                .Where(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Version.ToLowerInvariant())
                .OrderBy(v => v, new NuGetVersionComparer())
                .ToList();

            if (!versions.Any())
                return null;

            return new PackageVersionsIndex { Versions = versions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package versions for {PackageId}", packageId);
            return null;
        }
    }

    public async Task<Stream?> GetPackageContentAsync(string packageId, string version)
    {
        try
        {
            return await _storageService.GetPackageStreamAsync(packageId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package content for {PackageId} {Version}", packageId, version);
            return null;
        }
    }

    public async Task<Stream?> GetPackageManifestAsync(string packageId, string version)
    {
        try
        {
            // For now, return the same as package content
            // In a full implementation, you'd extract and return just the .nuspec
            return await _storageService.GetPackageStreamAsync(packageId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package manifest for {PackageId} {Version}", packageId, version);
            return null;
        }
    }

    public async Task<RegistrationIndex?> GetRegistrationIndexAsync(string packageId, string baseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl is required for generating NuGet V3 protocol URLs. Use IBaseUrlResolver to provide it.", nameof(baseUrl));
            }

            var packages = await _storageService.GetPackagesAsync(packageId, 0, int.MaxValue);
            var packageVersions = packages
                .Where(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Version, new NuGetVersionComparer())
                .ToList();

            if (!packageVersions.Any())
                return null;
            var registrationId = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/index.json";

            // Create a single page for all versions (simplified approach)
            var page = new RegistrationPage
            {
                Id = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/page.json",
                Type = "catalog:CatalogPage",
                Count = packageVersions.Count,
                Lower = packageVersions.First().Version.ToLowerInvariant(),
                Upper = packageVersions.Last().Version.ToLowerInvariant(),
                Items = packageVersions.Select(p => ConvertToRegistrationLeaf(p, baseUrl)).ToList()
            };

            return new RegistrationIndex
            {
                Id = registrationId,
                Count = 1,
                Items = new List<RegistrationPage> { page }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registration index for {PackageId}", packageId);
            return null;
        }
    }

    public async Task<RegistrationPage?> GetRegistrationPageAsync(string packageId, string lower, string upper, string baseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl is required for generating NuGet V3 protocol URLs. Use IBaseUrlResolver to provide it.", nameof(baseUrl));
            }

            var packages = await _storageService.GetPackagesAsync(packageId, 0, int.MaxValue);
            var packageVersions = packages
                .Where(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
                .Where(p => IsVersionInRange(p.Version, lower, upper))
                .OrderBy(p => p.Version, new NuGetVersionComparer())
                .ToList();

            if (!packageVersions.Any())
                return null;

            return new RegistrationPage
            {
                Id = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/page/{lower}/{upper}.json",
                Type = "catalog:CatalogPage",
                Count = packageVersions.Count,
                Lower = lower,
                Upper = upper,
                Items = packageVersions.Select(p => ConvertToRegistrationLeaf(p, baseUrl)).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registration page for {PackageId}", packageId);
            return null;
        }
    }

    public async Task<RegistrationLeaf?> GetRegistrationLeafAsync(string packageId, string version, string baseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl is required for generating NuGet V3 protocol URLs. Use IBaseUrlResolver to provide it.", nameof(baseUrl));
            }

            var package = await _storageService.GetPackageAsync(packageId, version);
            if (package == null)
                return null;
            return ConvertToRegistrationLeaf(package, baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registration leaf for {PackageId} {Version}", packageId, version);
            return null;
        }
    }

    public async Task<bool> PackageExistsAsync(string packageId, string version)
    {
        var package = await _storageService.GetPackageAsync(packageId, version);
        return package != null;
    }

    public async Task<IEnumerable<string>> GetAllPackageIdsAsync()
    {
        var packages = await _storageService.GetPackagesAsync(null, 0, int.MaxValue);
        return packages.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<string>> GetPackageVersionsListAsync(string packageId)
    {
        var packages = await _storageService.GetPackagesAsync(packageId, 0, int.MaxValue);
        return packages
            .Where(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Version)
            .OrderBy(v => v, new NuGetVersionComparer());
    }

    private static bool IsPrerelease(string version)
    {
        return version.Contains('-');
    }

    private static bool IsVersionInRange(string version, string lower, string upper)
    {
        try
        {
            var versionParsed = NuGetVersion.Parse(version);
            var lowerParsed = NuGetVersion.Parse(lower);
            var upperParsed = NuGetVersion.Parse(upper);

            return versionParsed.CompareTo(lowerParsed) >= 0 &&
                   versionParsed.CompareTo(upperParsed) <= 0;
        }
        catch
        {
            return false;
        }
    }

    private SearchResult ConvertToSearchResult(string packageId, List<PackageMetadata> versions, string baseUrl)
    {
        var latest = versions.OrderByDescending(v => v.Version, new NuGetVersionComparer()).First();

        return new SearchResult
        {
            Id = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/index.json",
            Type = "Package",
            Registration = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/index.json",
            PackageId = packageId,
            Version = latest.Version,
            Description = latest.Description,
            Summary = latest.Summary,
            Title = latest.Title ?? packageId,
            IconUrl = latest.IconUrl,
            LicenseUrl = latest.LicenseUrl,
            ProjectUrl = latest.ProjectUrl,
            Tags = ParseTags(latest.Tags),
            Authors = ParseAuthors(latest.Authors),
            TotalDownloads = 0, // TODO: Implement download tracking
            Verified = false,
            PackageTypes = new List<PackageType> { new() { Name = "Dependency" } },
            Versions = versions.Select(v => new SearchVersion
            {
                Version = v.Version,
                Downloads = 0,
                Id = $"{baseUrl}/v3/registrations/{packageId.ToLowerInvariant()}/{v.Version.ToLowerInvariant()}.json"
            }).ToList()
        };
    }

    private RegistrationLeaf ConvertToRegistrationLeaf(PackageMetadata package, string baseUrl)
    {
        var packageIdLower = package.Id.ToLowerInvariant();
        var versionLower = package.Version.ToLowerInvariant();

        return new RegistrationLeaf
        {
            Id = $"{baseUrl}/v3/registrations/{packageIdLower}/{versionLower}.json",
            Type = "Package",
            Registration = $"{baseUrl}/v3/registrations/{packageIdLower}/index.json",
            PackageContent = $"{baseUrl}/v3-flatcontainer/{packageIdLower}/{versionLower}/{packageIdLower}.{versionLower}.nupkg",
            CatalogEntry = new CatalogEntry
            {
                Id = $"{baseUrl}/v3/registrations/{packageIdLower}/{versionLower}.json",
                Type = "PackageDetails",
                Authors = package.Authors,
                Description = package.Description,
                IconUrl = package.IconUrl,
                Id_Package = package.Id,
                Language = package.Language,
                LicenseUrl = package.LicenseUrl,
                Listed = true,
                PackageContent = $"{baseUrl}/v3-flatcontainer/{packageIdLower}/{versionLower}/{packageIdLower}.{versionLower}.nupkg",
                ProjectUrl = package.ProjectUrl,
                Published = package.Published,
                RequireLicenseAcceptance = package.RequireLicenseAcceptance == "true",
                Summary = package.Summary,
                Tags = ParseTags(package.Tags),
                Title = package.Title,
                Version = package.Version,
                DependencyGroups = ParseDependencies(package.Dependencies)
            }
        };
    }

    private static List<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return new List<string>();

        return tags.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(t => t.Trim())
                  .Where(t => !string.IsNullOrEmpty(t))
                  .ToList();
    }

    private static List<string> ParseAuthors(string? authors)
    {
        if (string.IsNullOrWhiteSpace(authors))
            return new List<string>();

        return authors.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(a => a.Trim())
                     .Where(a => !string.IsNullOrEmpty(a))
                     .ToList();
    }

    private static List<DependencyGroup> ParseDependencies(string? dependencies)
    {
        if (string.IsNullOrWhiteSpace(dependencies))
            return new List<DependencyGroup>();

        // Simple parsing - in production you'd want more robust parsing
        var deps = dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(d => d.Trim())
                              .Where(d => !string.IsNullOrEmpty(d))
                              .Select(d => new Dependency
                              {
                                  Id = $"https://api.nuget.org/v3/registrations2/{d.ToLowerInvariant()}/index.json",
                                  PackageId = d,
                                  Range = "[1.0.0, )",
                                  Registration = $"https://api.nuget.org/v3/registrations2/{d.ToLowerInvariant()}/index.json"
                              })
                              .ToList();

        return new List<DependencyGroup>
        {
            new()
            {
                Dependencies = deps,
                TargetFramework = ".NETStandard2.0"
            }
        };
    }

    private class NuGetVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            try
            {
                var versionX = NuGetVersion.Parse(x);
                var versionY = NuGetVersion.Parse(y);
                return versionX.CompareTo(versionY);
            }
            catch
            {
                return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
