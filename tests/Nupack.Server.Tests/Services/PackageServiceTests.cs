using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Services;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class PackageServiceTests
{
    private readonly Mock<IPackageStorageService> _mockStorageService;
    private readonly Mock<IPackageUploadValidator> _mockUploadValidator;
    private readonly Mock<IPackageLifecycleHook> _mockLifecycleHook;
    private readonly Mock<ILogger<PackageService>> _mockLogger;
    private readonly PackageService _packageService;

    public PackageServiceTests()
    {
        _mockStorageService = new Mock<IPackageStorageService>();
        _mockUploadValidator = new Mock<IPackageUploadValidator>();
        _mockLifecycleHook = new Mock<IPackageLifecycleHook>();
        _mockLogger = new Mock<ILogger<PackageService>>();

        _mockUploadValidator
            .Setup(x => x.ValidateAsync(It.IsAny<PackageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PackageUploadValidationResult.Success());

        _packageService = new PackageService(
            _mockStorageService.Object,
            _mockUploadValidator.Object,
            _mockLifecycleHook.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UploadPackageAsync_WithValidPackage_ReturnsSuccessAndInvokesLifecycleHook()
    {
        var packageFile = CreatePackageFile();
        var request = new PackageUploadRequest(packageFile.Object);
        var metadata = CreatePackageMetadata();

        _mockStorageService.Setup(x => x.StorePackageAsync(It.IsAny<PackageUploadContent>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);

        var result = await _packageService.UploadPackageAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(metadata);
        _mockStorageService.Verify(x => x.StorePackageAsync(It.Is<PackageUploadContent>(content => content.FileName == packageFile.Object.FileName && content.Length == packageFile.Object.Length), It.IsAny<CancellationToken>()), Times.Once);
        _mockLifecycleHook.Verify(x => x.OnPackageUploadedAsync(metadata, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPackageAsync_WhenValidatorRejectsPackage_ReturnsFailureAndSkipsStorage()
    {
        var packageFile = CreatePackageFile(fileName: "not-a-package.txt");
        var request = new PackageUploadRequest(packageFile.Object);

        _mockUploadValidator
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PackageUploadValidationResult.Fail("Only .nupkg files are supported"));

        var result = await _packageService.UploadPackageAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Only .nupkg files are supported");
        _mockStorageService.Verify(x => x.StorePackageAsync(It.IsAny<PackageUploadContent>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLifecycleHook.Verify(x => x.OnPackageUploadedAsync(It.IsAny<PackageMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchPackagesAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        var request = new PackageSearchRequest("test", 0, 10);
        var expectedPackages = new List<PackageMetadata> { CreatePackageMetadata() };

        _mockStorageService.Setup(x => x.GetPackagesAsync("test", 0, 10, It.IsAny<CancellationToken>())).ReturnsAsync(expectedPackages);
        _mockStorageService.Setup(x => x.GetTotalPackageCountAsync("test", It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _packageService.SearchPackagesAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Packages.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchPackagesAsync_WhenStorageThrowsException_ReturnsFailureResponse()
    {
        var request = new PackageSearchRequest("test", 0, 10);
        _mockStorageService
            .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage error"));

        var result = await _packageService.SearchPackagesAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to search packages");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetPackageAsync_WithExistingPackage_ReturnsSuccessResponse()
    {
        var metadata = CreatePackageMetadata();
        _mockStorageService.Setup(x => x.GetPackageAsync(metadata.Id, metadata.Version, It.IsAny<CancellationToken>())).ReturnsAsync(metadata);

        var result = await _packageService.GetPackageAsync(metadata.Id, metadata.Version);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(metadata);
    }

    [Fact]
    public async Task GetPackageAsync_WithNonExistentPackage_ReturnsNotFoundResponse()
    {
        _mockStorageService.Setup(x => x.GetPackageAsync("NonExistentPackage", "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackageMetadata?)null);

        var result = await _packageService.GetPackageAsync("NonExistentPackage", "1.0.0");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Package not found");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task DeletePackageAsync_WithExistingPackage_ReturnsSuccessAndInvokesLifecycleHook()
    {
        _mockStorageService.Setup(x => x.DeletePackageAsync("TestPackage", "1.0.0", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _packageService.DeletePackageAsync("TestPackage", "1.0.0");

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Package deleted successfully");
        _mockLifecycleHook.Verify(x => x.OnPackageDeletedAsync("TestPackage", "1.0.0", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePackageAsync_WithNonExistentPackage_ReturnsNotFoundResponse()
    {
        _mockStorageService.Setup(x => x.DeletePackageAsync("NonExistentPackage", "1.0.0", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _packageService.DeletePackageAsync("NonExistentPackage", "1.0.0");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Package not found");
        result.Data.Should().BeFalse();
        _mockLifecycleHook.Verify(x => x.OnPackageDeletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IFormFile> CreatePackageFile(string fileName = "TestPackage.1.0.0.nupkg", long length = 1024)
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns(fileName);
        file.SetupGet(x => x.Length).Returns(length);
        file.Setup(x => x.OpenReadStream()).Returns(stream);
        return file;
    }

    private static PackageMetadata CreatePackageMetadata()
    {
        return new PackageMetadata(
            "TestPackage",
            "1.0.0",
            "Test Package",
            "A test package",
            "A test package",
            "Test Author",
            "Test Owner",
            "test",
            "Release notes",
            "Copyright",
            "en",
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            1024,
            "TestPackage.1.0.0.nupkg",
            false,
            true,
            true);
    }
}
