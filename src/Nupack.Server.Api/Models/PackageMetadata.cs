using Nupack.Server.Storage.Models;

namespace Nupack.Server.Api.Models;

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
