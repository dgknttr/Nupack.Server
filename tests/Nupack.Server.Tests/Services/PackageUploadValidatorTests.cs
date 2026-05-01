using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Services;
using Nupack.Server.Storage;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class PackageUploadValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithPackageAtConfiguredLimit_ReturnsSuccess()
    {
        var validator = CreateValidator(maxPackageSizeBytes: 3);
        var request = new PackageUploadRequest(CreatePackageFile(length: 3));

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithPackageBelowConfiguredLimit_ReturnsSuccess()
    {
        var validator = CreateValidator(maxPackageSizeBytes: 4);
        var request = new PackageUploadRequest(CreatePackageFile(length: 3));

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithPackageAboveConfiguredLimit_ReturnsFailure()
    {
        var validator = CreateValidator(maxPackageSizeBytes: 3);
        var request = new PackageUploadRequest(CreatePackageFile(length: 4));

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Package file size cannot exceed 3 bytes.");
    }

    private static DefaultPackageUploadValidator CreateValidator(long maxPackageSizeBytes)
    {
        return new DefaultPackageUploadValidator(Options.Create(new PackageUploadOptions
        {
            MaxPackageSizeBytes = maxPackageSizeBytes
        }));
    }

    private static IFormFile CreatePackageFile(long length)
    {
        return new FormFile(new MemoryStream(new byte[length]), 0, length, "package", "TestPackage.1.0.0.nupkg");
    }
}
