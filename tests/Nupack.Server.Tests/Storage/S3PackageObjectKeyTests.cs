using FluentAssertions;
using Nupack.Server.Storage.S3;
using Xunit;

namespace Nupack.Server.Tests.Storage;

public class S3PackageObjectKeyTests
{
    [Theory]
    [InlineData("", "TestPackage", "1.2.3", "testpackage/1.2.3/testpackage.1.2.3.nupkg")]
    [InlineData("packages", "TestPackage", "1.2.3", "packages/testpackage/1.2.3/testpackage.1.2.3.nupkg")]
    [InlineData("packages/", "TestPackage", "1.2.3-beta.1", "packages/testpackage/1.2.3-beta.1/testpackage.1.2.3-beta.1.nupkg")]
    public void Build_ReturnsExpectedObjectKey(string prefix, string packageId, string version, string expected)
    {
        var key = S3PackageObjectKey.Build(prefix, packageId, version);

        key.Should().Be(expected);
    }
}
