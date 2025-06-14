using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public interface IPackageService
{
    Task<ApiResponse<PackageMetadata>> UploadPackageAsync(PackageUploadRequest request);
    Task<ApiResponse<PackageListResponse>> SearchPackagesAsync(PackageSearchRequest request);
    Task<ApiResponse<PackageMetadata>> GetPackageAsync(string id, string version);
    Task<ApiResponse<Stream>> DownloadPackageAsync(string id, string version);
    Task<ApiResponse<bool>> DeletePackageAsync(string id, string version);
}
