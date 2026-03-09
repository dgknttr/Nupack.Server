using FluentAssertions;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using Nupack.Server.Storage.Models;
using Xunit;

namespace Nupack.Server.Tests.Storage;

public class PackageStorageOptionsValidatorTests
{
    private readonly PackageStorageOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultFileSystemSettings_Succeeds()
    {
        var result = _validator.Validate(Options.DefaultName, new PackageStorageOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithS3ProviderAndMissingFields_Fails()
    {
        var options = new PackageStorageOptions
        {
            Provider = PackageStorageProvider.S3,
            S3 = new S3PackageStorageOptions()
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("BucketName"));
        result.Failures.Should().Contain(failure => failure.Contains("Region"));
        result.Failures.Should().Contain(failure => failure.Contains("AccessKey"));
        result.Failures.Should().Contain(failure => failure.Contains("SecretKey"));
    }
}
