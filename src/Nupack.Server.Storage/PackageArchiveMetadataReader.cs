using NuGet.Packaging;
using Nupack.Server.Storage.Models;

namespace Nupack.Server.Storage.Services;

public sealed class PackageArchiveMetadataReader
{
    public Task<PackageMetadata> ReadMetadataAsync(PackageUploadContent package, CancellationToken cancellationToken = default)
    {
        return ReadMetadataAsync(package.OpenReadStream, package.FileName, package.Length, DateTime.UtcNow, cancellationToken);
    }

    public async Task<PackageMetadata> ReadMetadataAsync(Func<Stream> openReadStream, string fileName, long size, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        using var sourceStream = openReadStream();
        return await ReadMetadataAsync(sourceStream, fileName, size, timestampUtc, cancellationToken);
    }

    public async Task<PackageMetadata> ReadMetadataAsync(Stream sourceStream, string fileName, long size, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        using var packageStream = await EnsureSeekableStreamAsync(sourceStream, cancellationToken);
        using var packageReader = new PackageArchiveReader(packageStream, leaveStreamOpen: false);

        using var nuspecStream = await packageReader.GetNuspecAsync(cancellationToken);
        var manifest = Manifest.ReadFrom(nuspecStream, validateSchema: false);
        var metadata = manifest.Metadata;
        var version = metadata.Version.ToString();

        return new PackageMetadata(
            Id: metadata.Id,
            Version: version,
            Title: metadata.Title,
            Description: metadata.Description,
            Summary: metadata.Summary,
            Authors: string.Join(", ", metadata.Authors ?? Enumerable.Empty<string>()),
            Owners: string.Join(", ", metadata.Owners ?? Enumerable.Empty<string>()),
            Tags: metadata.Tags,
            ReleaseNotes: metadata.ReleaseNotes,
            Copyright: metadata.Copyright,
            Language: metadata.Language,
            IconUrl: metadata.IconUrl?.ToString(),
            ProjectUrl: metadata.ProjectUrl?.ToString(),
            LicenseUrl: metadata.LicenseUrl?.ToString(),
            RequireLicenseAcceptance: metadata.RequireLicenseAcceptance.ToString().ToLowerInvariant(),
            Dependencies: BuildDependencyList(metadata.DependencyGroups),
            Created: timestampUtc,
            Published: timestampUtc,
            Size: size,
            FileName: fileName,
            IsPrerelease: version.Contains('-', StringComparison.Ordinal),
            IsLatestVersion: false,
            IsAbsoluteLatestVersion: false);
    }

    private static async Task<Stream> EnsureSeekableStreamAsync(Stream sourceStream, CancellationToken cancellationToken)
    {
        if (sourceStream.CanSeek)
        {
            sourceStream.Position = 0;
            return sourceStream;
        }

        var bufferedStream = new MemoryStream();
        await sourceStream.CopyToAsync(bufferedStream, cancellationToken);
        bufferedStream.Position = 0;
        return bufferedStream;
    }

    private static string BuildDependencyList(IEnumerable<PackageDependencyGroup>? dependencyGroups)
    {
        if (dependencyGroups?.Any() != true)
        {
            return string.Empty;
        }

        var dependencyIds = dependencyGroups
            .SelectMany(group => group.Packages?.Select(package => package.Id) ?? Enumerable.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", dependencyIds);
    }
}


