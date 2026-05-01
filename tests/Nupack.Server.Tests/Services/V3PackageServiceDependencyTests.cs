using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nupack.Server.Api.Services;
using Nupack.Server.Storage;
using Nupack.Server.Storage.Models;
using Nupack.Server.Storage.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class V3PackageServiceDependencyTests
{
    [Fact]
    public async Task MetadataReader_RetainsDependencyGroupsWithFrameworkAndRange()
    {
        using var packageStream = CreatePackageStream();
        var reader = new PackageArchiveMetadataReader();

        var metadata = await reader.ReadMetadataAsync(packageStream, "Dependent.Package.1.2.3.nupkg", packageStream.Length, DateTime.UtcNow);

        metadata.Dependencies.Should().Be("Newtonsoft.Json");
        metadata.DependencyGroups.Should().HaveCount(2);
        var group = metadata.DependencyGroups.Should().Contain(g => g.TargetFramework == "netstandard2.0").Subject;
        group.TargetFramework.Should().Be("netstandard2.0");
        group.Dependencies.Should().ContainSingle();
        group.Dependencies[0].Id.Should().Be("Newtonsoft.Json");
        group.Dependencies[0].VersionRange.Should().NotBeNullOrWhiteSpace();
        group.Dependencies[0].VersionRange.Should().Contain("13.0.1");
        group.Dependencies[0].VersionRange.Should().Contain("14.0.0");

        var emptyGroup = metadata.DependencyGroups.Should().Contain(g => g.TargetFramework == "net20").Subject;
        emptyGroup.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task RegistrationLeaf_UsesActualDependencyMetadataAndLocalRegistrationUrl()
    {
        using var packageStream = CreatePackageStream();
        var reader = new PackageArchiveMetadataReader();
        var metadata = await reader.ReadMetadataAsync(packageStream, "Dependent.Package.1.2.3.nupkg", packageStream.Length, DateTime.UtcNow);

        var storage = new Mock<IPackageStorageService>();
        storage
            .Setup(service => service.GetPackageAsync("Dependent.Package", "1.2.3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var service = new V3PackageService(storage.Object, NullLogger<V3PackageService>.Instance);
        var leaf = await service.GetRegistrationLeafAsync("Dependent.Package", "1.2.3", "https://packages.example.test");

        leaf.Should().NotBeNull();
        leaf!.CatalogEntry.DependencyGroups.Should().HaveCount(2);
        var group = leaf.CatalogEntry.DependencyGroups.Should().Contain(g => g.TargetFramework == "netstandard2.0").Subject;
        group.TargetFramework.Should().Be("netstandard2.0");

        var dependency = group.Dependencies.Should().ContainSingle().Subject;
        dependency.PackageId.Should().Be("Newtonsoft.Json");
        var dependencyMetadata = metadata.DependencyGroups.Single(g => g.TargetFramework == "netstandard2.0").Dependencies[0];
        dependency.Range.Should().Be(dependencyMetadata.VersionRange);
        dependency.Range.Should().NotBe("[1.0.0, )");
        dependency.Id.Should().Be("https://packages.example.test/v3/registrations/newtonsoft.json/index.json");
        dependency.Registration.Should().Be("https://packages.example.test/v3/registrations/newtonsoft.json/index.json");

        var emptyGroup = leaf.CatalogEntry.DependencyGroups.Should().Contain(g => g.TargetFramework == "net20").Subject;
        emptyGroup.Dependencies.Should().BeEmpty();
    }

    private static MemoryStream CreatePackageStream()
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var nuspec = archive.CreateEntry("Dependent.Package.nuspec");
            using (var writer = new StreamWriter(nuspec.Open()))
            {
                writer.Write("""
                    <?xml version="1.0" encoding="utf-8"?>
                    <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                      <metadata>
                        <id>Dependent.Package</id>
                        <version>1.2.3</version>
                        <authors>Test Author</authors>
                        <description>Package with dependency metadata.</description>
                        <dependencies>
                          <group targetFramework="netstandard2.0">
                            <dependency id="Newtonsoft.Json" version="[13.0.1,14.0.0)" />
                          </group>
                          <group targetFramework="net20"></group>
                        </dependencies>
                      </metadata>
                    </package>
                    """);
            }

            archive.CreateEntry("lib/netstandard2.0/_._");
        }

        stream.Position = 0;
        return stream;
    }
}
