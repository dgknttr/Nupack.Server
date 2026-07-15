using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;
using Nupack.Server.Storage.S3;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using Xunit;

namespace Nupack.Server.Tests.Storage;

public class S3PackageStorageServiceTests
{
    [Fact]
    public async Task CheckHealthAsync_UsesBoundedBucketRequest()
    {
        var client = new Mock<IAmazonS3>();
        client.Setup(value => value.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response());
        var storage = CreateStorage(client.Object);
        client.Invocations.Clear();

        await storage.CheckHealthAsync();

        client.Verify(value => value.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(request => request.BucketName == "packages" && request.MaxKeys == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenS3Fails_PropagatesFailure()
    {
        var client = new Mock<IAmazonS3>();
        client.Setup(value => value.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response());
        var storage = CreateStorage(client.Object);
        client.Setup(value => value.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("secret credential must not escape"));

        var action = () => storage.CheckHealthAsync();

        await action.Should().ThrowAsync<AmazonS3Exception>();
    }

    [Fact]
    public async Task CheckHealthAsync_PassesCancellationToS3()
    {
        var client = new Mock<IAmazonS3>();
        client.Setup(value => value.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response());
        var storage = CreateStorage(client.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        client.Setup(value => value.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var action = () => storage.CheckHealthAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

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
            var storedPackage = await storage.StorePackageAsync(new PackageUploadContent(Path.GetFileName(packagePath), new FileInfo(packagePath).Length, () => File.OpenRead(packagePath)));
            var objectKey = S3PackageObjectKey.Build(environment.Prefix ?? string.Empty, storedPackage.Id, storedPackage.Version);
            await environment.WaitForObjectVisibilityAsync(client, bucketName, objectKey);

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

    private static S3PackageStorageService CreateStorage(IAmazonS3 client)
    {
        var options = new PackageStorageOptions
        {
            Provider = PackageStorageProvider.S3,
            S3 = new S3PackageStorageOptions
            {
                BucketName = "packages",
                Region = "us-east-1",
                AccessKey = "access",
                SecretKey = "secret"
            }
        };

        return new S3PackageStorageService(client, Options.Create(options), new PackageArchiveMetadataReader(), NullLogger<S3PackageStorageService>.Instance);
    }
}
