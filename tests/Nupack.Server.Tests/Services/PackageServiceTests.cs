using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class PackageServiceTests
{
    private readonly Mock<IPackageStorageService> _mockStorageService;
    private readonly Mock<ILogger<PackageService>> _mockLogger;
    private readonly PackageService _packageService;

    public PackageServiceTests()
    {
        _mockStorageService = new Mock<IPackageStorageService>();
        _mockLogger = new Mock<ILogger<PackageService>>();
        _packageService = new PackageService(_mockStorageService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SearchPackagesAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new PackageSearchRequest("test", 0, 10);
        var expectedPackages = new List<PackageMetadata>
        {
            new("TestPackage", "1.0.0", "Test Package", "A test package", "A test package", "Test Author", "Test Owner", "test", "Release notes", "Copyright", "en", null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, 1024, "TestPackage.1.0.0.nupkg", false, true, true)
        };
        var expectedResponse = new PackageListResponse(expectedPackages, 1);

        _mockStorageService.Setup(x => x.GetPackagesAsync("test", 0, 10))
            .ReturnsAsync(expectedPackages);
        _mockStorageService.Setup(x => x.GetTotalPackageCountAsync("test"))
            .ReturnsAsync(1);

        // Act
        var result = await _packageService.SearchPackagesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Packages.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchPackagesAsync_WhenStorageThrowsException_ReturnsFailureResponse()
    {
        // Arrange
        var request = new PackageSearchRequest("test", 0, 10);
        _mockStorageService.Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _packageService.SearchPackagesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to search packages");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetPackageAsync_WithExistingPackage_ReturnsSuccessResponse()
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var expectedPackage = new PackageMetadata(
            packageId, version, "Test Package", "A test package", "A test package",
            "Test Author", "Test Owner", "test", "Release notes", "Copyright", "en", null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, 1024, "TestPackage.1.0.0.nupkg", false, true, true);

        _mockStorageService.Setup(x => x.GetPackageAsync(packageId, version))
            .ReturnsAsync(expectedPackage);

        // Act
        var result = await _packageService.GetPackageAsync(packageId, version);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(packageId);
        result.Data.Version.Should().Be(version);
    }

    [Fact]
    public async Task GetPackageAsync_WithNonExistentPackage_ReturnsNotFoundResponse()
    {
        // Arrange
        var packageId = "NonExistentPackage";
        var version = "1.0.0";

        _mockStorageService.Setup(x => x.GetPackageAsync(packageId, version))
            .ReturnsAsync((PackageMetadata?)null);

        // Act
        var result = await _packageService.GetPackageAsync(packageId, version);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Package not found");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task DeletePackageAsync_WithExistingPackage_ReturnsSuccessResponse()
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";

        _mockStorageService.Setup(x => x.DeletePackageAsync(packageId, version))
            .ReturnsAsync(true);

        // Act
        var result = await _packageService.DeletePackageAsync(packageId, version);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Package deleted successfully");
    }

    [Fact]
    public async Task DeletePackageAsync_WithNonExistentPackage_ReturnsNotFoundResponse()
    {
        // Arrange
        var packageId = "NonExistentPackage";
        var version = "1.0.0";

        _mockStorageService.Setup(x => x.DeletePackageAsync(packageId, version))
            .ReturnsAsync(false);

        // Act
        var result = await _packageService.DeletePackageAsync(packageId, version);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Package not found");
        result.Data.Should().BeFalse();
    }
}
