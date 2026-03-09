using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Storage.FileSystem;

public sealed class FileSystemPackageStorageService : IPackageStorageService
{
    private readonly string _packagesPath;
    private readonly PackageArchiveMetadataReader _metadataReader;
    private readonly ILogger<FileSystemPackageStorageService> _logger;
    private readonly ConcurrentDictionary<string, PackageMetadata> _packageCache = new(StringComparer.OrdinalIgnoreCase);

    public FileSystemPackageStorageService(
        IWebHostEnvironment environment,
        IOptions<PackageStorageOptions> storageOptions,
        PackageArchiveMetadataReader metadataReader,
        ILogger<FileSystemPackageStorageService> logger)
    {
        _metadataReader = metadataReader;
        _logger = logger;

        var options = storageOptions.Value.GetResolvedFileSystemOptions();
        _packagesPath = options.GetResolvedPath(environment.WebRootPath, environment.ContentRootPath);

        Directory.CreateDirectory(_packagesPath);
        _logger.LogInformation("File system package storage path resolved to {PackagesPath}", _packagesPath);
        InitializeCache();
    }

    public async Task<PackageMetadata> StorePackageAsync(PackageUploadContent package, CancellationToken cancellationToken = default)
    {
        var metadata = await _metadataReader.ReadMetadataAsync(package, cancellationToken);
        var fileName = $"{metadata.Id}.{metadata.Version}.nupkg";
        var filePath = Path.Combine(_packagesPath, fileName);

        if (File.Exists(filePath))
        {
            throw new InvalidOperationException($"Package {metadata.Id} {metadata.Version} already exists");
        }

        await using (var sourceStream = package.OpenReadStream())
        await using (var destinationStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }

        var storedMetadata = metadata with
        {
            FileName = fileName,
            Size = package.Length,
            Created = DateTime.UtcNow,
            Published = DateTime.UtcNow
        };

        _packageCache[BuildPackageKey(storedMetadata.Id, storedMetadata.Version)] = storedMetadata;
        _logger.LogInformation("Stored package {PackageId} {Version} on the file system.", storedMetadata.Id, storedMetadata.Version);

        return storedMetadata;
    }

    public Task<IEnumerable<PackageMetadata>> GetPackagesAsync(string? searchQuery = null, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var packages = _packageCache.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            packages = packages.Where(package =>
                package.Id.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                (package.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (package.Tags?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var page = packages
            .OrderByDescending(package => package.Created)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IEnumerable<PackageMetadata>>(page);
    }

    public Task<PackageMetadata?> GetPackageAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        _packageCache.TryGetValue(BuildPackageKey(id, version), out var package);
        return Task.FromResult(package);
    }

    public async Task<Stream?> GetPackageStreamAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        var package = await GetPackageAsync(id, version, cancellationToken);
        if (package is null)
        {
            return null;
        }

        var filePath = Path.Combine(_packagesPath, package.FileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<bool> DeletePackageAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        var package = await GetPackageAsync(id, version, cancellationToken);
        if (package is null)
        {
            return false;
        }

        var filePath = Path.Combine(_packagesPath, package.FileName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        _packageCache.TryRemove(BuildPackageKey(id, version), out _);
        _logger.LogInformation("Deleted package {PackageId} {Version} from the file system.", id, version);

        return true;
    }

    public async Task<int> GetTotalPackageCountAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var packages = await GetPackagesAsync(searchQuery, 0, int.MaxValue, cancellationToken);
        return packages.Count();
    }

    private void InitializeCache()
    {
        try
        {
            foreach (var filePath in Directory.GetFiles(_packagesPath, "*.nupkg"))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var metadata = _metadataReader
                        .ReadMetadataAsync(() => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), fileInfo.Name, fileInfo.Length, fileInfo.CreationTimeUtc)
                        .GetAwaiter()
                        .GetResult();

                    _packageCache[BuildPackageKey(metadata.Id, metadata.Version)] = metadata;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load package metadata from {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Loaded {PackageCount} packages from the file system cache.", _packageCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize file system package cache.");
            throw;
        }
    }

    private static string BuildPackageKey(string id, string version)
        => $"{id}:{version}";
}


