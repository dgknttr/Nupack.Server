using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Nupack.Server.Storage.Models;

namespace Nupack.Server.Tests.Storage;

internal static class TestPackageLocator
{
    public static string ResolveSamplePackagePath()
    {
        var current = AppContext.BaseDirectory;

        for (var depth = 0; depth < 8; depth++)
        {
            var candidate = Path.Combine(current, "test", "TestPackage.1.0.0.nupkg");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new FileNotFoundException("Could not locate TestPackage.1.0.0.nupkg for storage tests.");
    }
}

internal sealed class S3TestEnvironment
{
    private S3TestEnvironment(string serviceUrl, string region, string accessKey, string secretKey, bool forcePathStyle, string? prefix)
    {
        ServiceUrl = serviceUrl;
        Region = region;
        AccessKey = accessKey;
        SecretKey = secretKey;
        ForcePathStyle = forcePathStyle;
        Prefix = prefix;
    }

    public string ServiceUrl { get; }
    public string Region { get; }
    public string AccessKey { get; }
    public string SecretKey { get; }
    public bool ForcePathStyle { get; }
    public string? Prefix { get; }

    public static S3TestEnvironment? TryCreate()
    {
        var serviceUrl = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__SERVICE_URL");
        var region = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__REGION");
        var accessKey = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__SECRET_KEY");

        if (string.IsNullOrWhiteSpace(serviceUrl) ||
            string.IsNullOrWhiteSpace(region) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            return null;
        }

        var prefix = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__PREFIX");
        var forcePathStyleValue = Environment.GetEnvironmentVariable("NUPACK_S3_TESTS__FORCE_PATH_STYLE");
        var forcePathStyle = string.IsNullOrWhiteSpace(forcePathStyleValue) || bool.Parse(forcePathStyleValue);

        return new S3TestEnvironment(serviceUrl, region, accessKey, secretKey, forcePathStyle, prefix);
    }

    public IAmazonS3 CreateClient()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(Region),
            ServiceURL = ServiceUrl,
            ForcePathStyle = ForcePathStyle
        };

        return new AmazonS3Client(new BasicAWSCredentials(AccessKey, SecretKey), config);
    }

    public PackageStorageOptions CreateOptions(string bucketName)
    {
        return new PackageStorageOptions
        {
            Provider = PackageStorageProvider.S3,
            S3 = new S3PackageStorageOptions
            {
                BucketName = bucketName,
                Region = Region,
                ServiceUrl = ServiceUrl,
                AccessKey = AccessKey,
                SecretKey = SecretKey,
                ForcePathStyle = ForcePathStyle,
                Prefix = Prefix
            }
        };
    }

    public async Task EnsureBucketExistsAsync(IAmazonS3 client, string bucketName)
    {
        var existingBuckets = await client.ListBucketsAsync();
        if (existingBuckets.Buckets?.Any(bucket => string.Equals(bucket.BucketName, bucketName, StringComparison.Ordinal)) == true)
        {
            return;
        }

        await client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucketName,
            UseClientRegion = true
        });
    }

    public async Task DeleteBucketRecursiveAsync(IAmazonS3 client, string bucketName)
    {
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                ContinuationToken = continuationToken
            });

            if (response.S3Objects?.Count > 0)
            {
                await client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = response.S3Objects.Select(item => new KeyVersion { Key = item.Key }).ToList()
                });
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        await client.DeleteBucketAsync(bucketName);
    }
}
