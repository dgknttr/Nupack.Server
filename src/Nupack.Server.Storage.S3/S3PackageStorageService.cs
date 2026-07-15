using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;

namespace Nupack.Server.Storage.S3;

public sealed class S3PackageStorageService : IPackageStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly PackageArchiveMetadataReader _metadataReader;
    private readonly ILogger<S3PackageStorageService> _logger;
    private readonly string _bucketName;
    private readonly string _prefix;
    private readonly ConcurrentDictionary<string, PackageMetadata> _packageCache = new(StringComparer.OrdinalIgnoreCase);

    public S3PackageStorageService(
        IAmazonS3 s3Client,
        IOptions<PackageStorageOptions> storageOptions,
        PackageArchiveMetadataReader metadataReader,
        ILogger<S3PackageStorageService> logger)
    {
        _s3Client = s3Client;
        _metadataReader = metadataReader;
        _logger = logger;

        var options = storageOptions.Value.S3;
        _bucketName = options.BucketName ?? throw new InvalidOperationException("PackageStorage:S3:BucketName must be configured.");
        _prefix = options.GetNormalizedPrefix();

        InitializeCacheAsync().GetAwaiter().GetResult();
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = _prefix,
            MaxKeys = 1
        }, cancellationToken);
    }

    public async Task<PackageMetadata> StorePackageAsync(PackageUploadContent package, CancellationToken cancellationToken = default)
    {
        var metadata = await _metadataReader.ReadMetadataAsync(package, cancellationToken);
        var packageKey = BuildPackageKey(metadata.Id, metadata.Version);

        if (_packageCache.ContainsKey(packageKey))
        {
            throw new InvalidOperationException($"Package {metadata.Id} {metadata.Version} already exists");
        }

        var objectKey = S3PackageObjectKey.Build(_prefix, metadata.Id, metadata.Version);
        await using (var stream = package.OpenReadStream())
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = stream,
                AutoCloseStream = false,
                ContentType = "application/octet-stream"
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
        }

        var storedMetadata = metadata with
        {
            FileName = Path.GetFileName(objectKey),
            Size = package.Length,
            Created = DateTime.UtcNow,
            Published = DateTime.UtcNow
        };

        _packageCache[packageKey] = storedMetadata;
        _logger.LogInformation("Stored package {PackageId} {Version} in S3 object storage.", storedMetadata.Id, storedMetadata.Version);

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
        _packageCache.TryGetValue(BuildPackageKey(id, version), out var metadata);
        return Task.FromResult(metadata);
    }

    public async Task<Stream?> GetPackageStreamAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        var package = await GetPackageAsync(id, version, cancellationToken);
        if (package is null)
        {
            return null;
        }

        var objectKey = S3PackageObjectKey.Build(_prefix, id, version);

        try
        {
            using var response = await _s3Client.GetObjectAsync(_bucketName, objectKey, cancellationToken);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            return null;
        }
    }

    public async Task<bool> DeletePackageAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        var package = await GetPackageAsync(id, version, cancellationToken);
        if (package is null)
        {
            return false;
        }

        var objectKey = S3PackageObjectKey.Build(_prefix, id, version);
        await _s3Client.DeleteObjectAsync(_bucketName, objectKey, cancellationToken);
        _packageCache.TryRemove(BuildPackageKey(id, version), out _);
        _logger.LogInformation("Deleted package {PackageId} {Version} from S3 object storage.", id, version);

        return true;
    }

    public async Task<int> GetTotalPackageCountAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var packages = await GetPackagesAsync(searchQuery, 0, int.MaxValue, cancellationToken);
        return packages.Count();
    }

    private async Task InitializeCacheAsync()
    {
        try
        {
            string? continuationToken = null;

            do
            {
                var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = _prefix,
                    ContinuationToken = continuationToken
                });

                var packageObjects = response.S3Objects?
                    .Where(s3Object => S3PackageObjectKey.IsPackageObject(s3Object.Key))
                    .ToList() ?? new List<S3Object>();

                foreach (var s3Object in packageObjects)
                {
                    var objectKey = s3Object.Key;
                    if (string.IsNullOrWhiteSpace(objectKey))
                    {
                        continue;
                    }

                    try
                    {
                        using var objectResponse = await _s3Client.GetObjectAsync(_bucketName, objectKey);
                        var metadata = await _metadataReader.ReadMetadataAsync(
                            objectResponse.ResponseStream,
                            Path.GetFileName(objectKey),
                            s3Object.Size ?? 0L,
                            (s3Object.LastModified ?? DateTime.UtcNow).ToUniversalTime());

                        _packageCache[BuildPackageKey(metadata.Id, metadata.Version)] = metadata;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load package metadata from S3 object {ObjectKey}", objectKey);
                    }
                }

                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
            }
            while (!string.IsNullOrWhiteSpace(continuationToken));

            _logger.LogInformation("Loaded {PackageCount} packages from S3 object storage.", _packageCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize S3 package storage cache.");
            throw;
        }
    }

    private static bool IsNotFound(AmazonS3Exception exception)
        => exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
           string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase);

    private static string BuildPackageKey(string id, string version)
        => $"{id}:{version}";
}
