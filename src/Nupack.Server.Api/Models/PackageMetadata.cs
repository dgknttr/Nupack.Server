namespace Nupack.Server.Api.Models;

// Legacy V2 model - keeping for backward compatibility
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

public record PackageUploadRequest(
    IFormFile Package
);

public record PackageListResponse(
    IEnumerable<PackageMetadata> Packages,
    int TotalCount
);

public record PackageSearchRequest(
    string? Query = null,
    int Skip = 0,
    int Take = 20
);

public record ApiResponse<T>(
    bool Success,
    T? Data = default,
    string? Message = null
);
