using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using Nupack.Server.Storage.FileSystem;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;
using Xunit;

namespace Nupack.Server.Tests.Storage;

public class FileSystemPackageStorageServiceTests
{
    [Fact]
    public async Task StoreAndReadRoundTrip_WorksWithCaseInsensitiveLookup()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var storage = CreateStorage(rootPath);
            var packagePath = TestPackageLocator.ResolveSamplePackagePath();
            var package = CreatePackageContent(packagePath);

            var metadata = await storage.StorePackageAsync(package);
            var versions = (await storage.GetPackagesAsync("TestPackage", 0, 20)).ToList();
            var fetched = await storage.GetPackageAsync("testpackage", "1.0.0");
            await using var stream = await storage.GetPackageStreamAsync("TESTPACKAGE", "1.0.0");

            metadata.Id.Should().Be("TestPackage");
            versions.Should().ContainSingle();
            fetched.Should().NotBeNull();
            stream.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task StartupReload_LoadsExistingPackagesIntoCache()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var firstStorage = CreateStorage(rootPath);
            var packagePath = TestPackageLocator.ResolveSamplePackagePath();
            await firstStorage.StorePackageAsync(CreatePackageContent(packagePath));

            var rehydratedStorage = CreateStorage(rootPath);
            var count = await rehydratedStorage.GetTotalPackageCountAsync();
            var package = await rehydratedStorage.GetPackageAsync("TestPackage", "1.0.0");

            count.Should().Be(1);
            package.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPackageDirectoryIsWritable_SucceedsWithoutLeavingProbeArtifact()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var storage = CreateStorage(rootPath);
            var packagesPath = Path.Combine(rootPath, "packages");

            await storage.CheckHealthAsync();

            Directory.GetFiles(packagesPath).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPackageDirectoryIsUnavailable_Fails()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var storage = CreateStorage(rootPath);
            var packagesPath = Path.Combine(rootPath, "packages");
            Directory.Delete(packagesPath);
            await File.WriteAllTextAsync(packagesPath, "not a directory");

            var action = () => storage.CheckHealthAsync();

            await action.Should().ThrowAsync<IOException>();
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAlreadyCanceled_ThrowsWithoutLeavingProbeArtifact()
    {
        var rootPath = CreateTempRoot();
        try
        {
            var storage = CreateStorage(rootPath);
            var packagesPath = Path.Combine(rootPath, "packages");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var action = () => storage.CheckHealthAsync(cancellation.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            Directory.GetFiles(packagesPath, ".nupack-health-*").Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static FileSystemPackageStorageService CreateStorage(string rootPath)
    {
        var environment = new TestWebHostEnvironment(rootPath);
        var options = Options.Create(new PackageStorageOptions
        {
            Provider = PackageStorageProvider.FileSystem,
            FileSystem = new FileSystemPackageStorageOptions
            {
                BasePath = "packages"
            }
        });

        return new FileSystemPackageStorageService(environment, options, new PackageArchiveMetadataReader(), NullLogger<FileSystemPackageStorageService>.Instance);
    }

    private static string CreateTempRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "Nupack.Server.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    private static PackageUploadContent CreatePackageContent(string packagePath)
    {
        return new PackageUploadContent(Path.GetFileName(packagePath), new FileInfo(packagePath).Length, () => File.OpenRead(packagePath));
    }

    private sealed class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ApplicationName = "Nupack.Server.Tests";
            EnvironmentName = "Development";
            WebRootPath = null!;
            WebRootFileProvider = null!;
            ContentRootFileProvider = null!;
        }

        public string ApplicationName { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
    }
}
