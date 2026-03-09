using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;
using Nupack.Server.Storage.S3;
using Xunit;

namespace Nupack.Server.Tests.Storage;

public class S3PackageStorageServiceTests
{
    [Fact]
    public async Task StoreAndReloadRoundTrip_WorksAgainstConfiguredObjectStore()
    {
        var environment = S3TestEnvironment.TryCreate();
        if (environment is null)
        {
            return;
        }

        var bucketName = $"nupack-test-{Guid.NewGuid():N}";
        using var client = environment.CreateClient();
        await environment.EnsureBucketExistsAsync(client, bucketName);

        try
        {
            var storage = new S3PackageStorageService(client, Options.Create(environment.CreateOptions(bucketName)), new PackageArchiveMetadataReader(), NullLogger<S3PackageStorageService>.Instance);
            var packagePath = TestPackageLocator.ResolveSamplePackagePath();
            await storage.StorePackageAsync(new PackageUploadContent(Path.GetFileName(packagePath), new FileInfo(packagePath).Length, () => File.OpenRead(packagePath)));

            var rehydrated = new S3PackageStorageService(client, Options.Create(environment.CreateOptions(bucketName)), new PackageArchiveMetadataReader(), NullLogger<S3PackageStorageService>.Instance);
            var count = await rehydrated.GetTotalPackageCountAsync();
            var package = await rehydrated.GetPackageAsync("testpackage", "1.0.0");
            await using var stream = await rehydrated.GetPackageStreamAsync("TestPackage", "1.0.0");

            count.Should().Be(1);
            package.Should().NotBeNull();
            stream.Should().NotBeNull();
        }
        finally
        {
            await environment.DeleteBucketRecursiveAsync(client, bucketName);
        }
    }
}

