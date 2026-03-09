namespace Nupack.Server.Storage.Models;

public record PackageMetadata(
    string Id,
    string Version,
    string? Title,
    string? Description,
    string? Summary,
    string? Authors,
    string? Owners,
    string? Tags,
    string? ReleaseNotes,
    string? Copyright,
    string? Language,
    string? IconUrl,
    string? ProjectUrl,
    string? LicenseUrl,
    string? RequireLicenseAcceptance,
    string? Dependencies,
    DateTime Created,
    DateTime Published,
    long Size,
    string FileName,
    bool IsPrerelease,
    bool IsLatestVersion,
    bool IsAbsoluteLatestVersion
);
