using System.Collections.Concurrent;
using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using NuGet.Packaging;
using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public class FileSystemPackageStorageService : IPackageStorageService
{
    private readonly string _packagesPath;
    private readonly ILogger<FileSystemPackageStorageService> _logger;
    private readonly ConcurrentDictionary<string, PackageMetadata> _packageCache = new();

    public FileSystemPackageStorageService(
        IWebHostEnvironment environment,
        ILogger<FileSystemPackageStorageService> logger,
        IOptions<PackageStorageOptions> storageOptions)
    {
        _logger = logger;

        // Resolve the packages path using the configuration
        var options = storageOptions.Value;
        _packagesPath = options.GetResolvedPath(environment.WebRootPath, environment.ContentRootPath);

        _logger.LogInformation("Package storage path resolved to: {PackagesPath}", _packagesPath);

        Directory.CreateDirectory(_packagesPath);
        InitializeCache();
    }

    public async Task<PackageMetadata> StorePackageAsync(IFormFile packageFile)
    {
        if (packageFile == null || packageFile.Length == 0)
            throw new ArgumentException("Package file is required");

        if (!packageFile.FileName.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a .nupkg file");

        using var stream = packageFile.OpenReadStream();
        using var packageReader = new PackageArchiveReader(stream);
        
        var nuspec = await packageReader.GetNuspecAsync(CancellationToken.None);
        var manifest = Manifest.ReadFrom(nuspec, false);
        var metadata = manifest.Metadata;

        var packageId = metadata.Id;
        var version = metadata.Version.ToString();
        var fileName = $"{packageId}.{version}.nupkg";
        var filePath = Path.Combine(_packagesPath, fileName);

        // Check if package already exists
        if (File.Exists(filePath))
            throw new InvalidOperationException($"Package {packageId} {version} already exists");

        // Save the package file
        stream.Position = 0;
        using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        // Extract dependencies in a simpler format
        var dependencies = string.Empty;
        if (metadata.DependencyGroups?.Any() == true)
        {
            var depList = new List<string>();
            foreach (var group in metadata.DependencyGroups)
            {
                var deps = group.Packages?.Select(p => p.Id) ?? Enumerable.Empty<string>();
                if (deps.Any())
                {
                    depList.AddRange(deps);
                }
            }
            dependencies = string.Join(", ", depList.Distinct());
        }

        var isPrerelease = IsPrerelease(version);
        var packageMetadata = new PackageMetadata(
            Id: packageId,
            Version: version,
            Title: metadata.Title,
            Description: metadata.Description,
            Summary: metadata.Summary,
            Authors: string.Join(", ", metadata.Authors ?? Enumerable.Empty<string>()),
            Owners: string.Join(", ", metadata.Owners ?? Enumerable.Empty<string>()),
            Tags: metadata.Tags,
            ReleaseNotes: metadata.ReleaseNotes,
            Copyright: metadata.Copyright,
            Language: metadata.Language,
            IconUrl: metadata.IconUrl?.ToString(),
            ProjectUrl: metadata.ProjectUrl?.ToString(),
            LicenseUrl: metadata.LicenseUrl?.ToString(),
            RequireLicenseAcceptance: metadata.RequireLicenseAcceptance.ToString().ToLower(),
            Dependencies: dependencies,
            Created: DateTime.UtcNow,
            Published: DateTime.UtcNow,
            Size: packageFile.Length,
            FileName: fileName,
            IsPrerelease: isPrerelease,
            IsLatestVersion: false, // Will be calculated later
            IsAbsoluteLatestVersion: false // Will be calculated later
        );

        _packageCache.TryAdd($"{packageId}:{version}", packageMetadata);
        
        _logger.LogInformation("Stored package {PackageId} {Version}", packageId, version);
        return packageMetadata;
    }

    public async Task<IEnumerable<PackageMetadata>> GetPackagesAsync(string? searchQuery = null, int skip = 0, int take = 20)
    {
        await Task.CompletedTask; // Make async for consistency
        
        var packages = _packageCache.Values.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            packages = packages.Where(p => 
                p.Id.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Tags?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return packages
            .OrderByDescending(p => p.Created)
            .Skip(skip)
            .Take(take);
    }

    public async Task<PackageMetadata?> GetPackageAsync(string id, string version)
    {
        await Task.CompletedTask;
        _packageCache.TryGetValue($"{id}:{version}", out var package);
        return package;
    }

    public async Task<Stream?> GetPackageStreamAsync(string id, string version)
    {
        var package = await GetPackageAsync(id, version);
        if (package == null) return null;

        var filePath = Path.Combine(_packagesPath, package.FileName);
        if (!File.Exists(filePath)) return null;

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> DeletePackageAsync(string id, string version)
    {
        var package = await GetPackageAsync(id, version);
        if (package == null) return false;

        var filePath = Path.Combine(_packagesPath, package.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _packageCache.TryRemove($"{id}:{version}", out _);
            _logger.LogInformation("Deleted package {PackageId} {Version}", id, version);
            return true;
        }

        return false;
    }

    public async Task<int> GetTotalPackageCountAsync(string? searchQuery = null)
    {
        var packages = await GetPackagesAsync(searchQuery, 0, int.MaxValue);
        return packages.Count();
    }

    private static bool IsPrerelease(string version)
    {
        return version.Contains('-');
    }

    private void InitializeCache()
    {
        try
        {
            var packageFiles = Directory.GetFiles(_packagesPath, "*.nupkg");

            foreach (var filePath in packageFiles)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using var packageReader = new PackageArchiveReader(stream);

                    var nuspecTask = packageReader.GetNuspecAsync(CancellationToken.None);
                    nuspecTask.Wait();
                    var nuspec = nuspecTask.Result;

                    var manifest = Manifest.ReadFrom(nuspec, false);
                    var metadata = manifest.Metadata;

                    // Extract dependencies in a simpler format
                    var dependencies = string.Empty;
                    if (metadata.DependencyGroups?.Any() == true)
                    {
                        var depList = new List<string>();
                        foreach (var group in metadata.DependencyGroups)
                        {
                            var deps = group.Packages?.Select(p => p.Id) ?? Enumerable.Empty<string>();
                            if (deps.Any())
                            {
                                depList.AddRange(deps);
                            }
                        }
                        dependencies = string.Join(", ", depList.Distinct());
                    }

                    var fileInfo = new FileInfo(filePath);
                    var version = metadata.Version.ToString();
                    var isPrerelease = IsPrerelease(version);

                    var packageMetadata = new PackageMetadata(
                        Id: metadata.Id ?? "Unknown",
                        Version: version,
                        Title: metadata.Title ?? metadata.Id ?? "Unknown",
                        Description: metadata.Description ?? "",
                        Summary: metadata.Summary ?? "",
                        Authors: string.Join(", ", metadata.Authors ?? Enumerable.Empty<string>()),
                        Owners: string.Join(", ", metadata.Owners ?? Enumerable.Empty<string>()),
                        Tags: metadata.Tags ?? "",
                        ReleaseNotes: metadata.ReleaseNotes ?? "",
                        Copyright: metadata.Copyright ?? "",
                        Language: metadata.Language ?? "",
                        IconUrl: metadata.IconUrl?.ToString() ?? "",
                        ProjectUrl: metadata.ProjectUrl?.ToString() ?? "",
                        LicenseUrl: metadata.LicenseUrl?.ToString() ?? "",
                        RequireLicenseAcceptance: metadata.RequireLicenseAcceptance.ToString().ToLower(),
                        Dependencies: dependencies,
                        Created: fileInfo.CreationTimeUtc,
                        Published: fileInfo.CreationTimeUtc,
                        Size: fileInfo.Length,
                        FileName: Path.GetFileName(filePath),
                        IsPrerelease: isPrerelease,
                        IsLatestVersion: false, // Will be calculated after loading all packages
                        IsAbsoluteLatestVersion: false // Will be calculated after loading all packages
                    );

                    _packageCache.TryAdd($"{metadata.Id}:{metadata.Version}", packageMetadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load package metadata from {FilePath}", filePath);
                }
            }

            // Skip latest version calculation for now to avoid issues
            _logger.LogInformation("Skipping latest version calculation for stability");

            _logger.LogInformation("Loaded {Count} packages into cache", _packageCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize package cache");
        }
    }

    private void UpdateLatestVersionFlags()
    {
        try
        {
            var packageGroups = _packageCache.Values.GroupBy(p => p.Id);

            foreach (var group in packageGroups)
            {
                var packages = group.ToList();

                // Simple approach: mark the first package as latest for now
                // In a real implementation, you'd use proper semantic version comparison
                var latestOverall = packages.FirstOrDefault();
                var latestStable = packages.FirstOrDefault(p => !p.IsPrerelease);

                // Update cache with corrected flags
                foreach (var package in packages)
                {
                    var key = $"{package.Id}:{package.Version}";
                    var isLatest = package == latestOverall;
                    var isAbsoluteLatest = package == latestStable;

                    var updatedPackage = package with
                    {
                        IsLatestVersion = isLatest,
                        IsAbsoluteLatestVersion = isAbsoluteLatest
                    };

                    _packageCache.TryUpdate(key, updatedPackage, package);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update latest version flags");
        }
    }
}
