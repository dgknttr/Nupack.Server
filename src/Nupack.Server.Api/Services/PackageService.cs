using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public class PackageService : IPackageService
{
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<PackageService> _logger;

    public PackageService(IPackageStorageService storageService, ILogger<PackageService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ApiResponse<PackageMetadata>> UploadPackageAsync(PackageUploadRequest request)
    {
        try
        {
            if (request.Package == null || request.Package.Length == 0)
            {
                return new ApiResponse<PackageMetadata>(false, Message: "Package file is required");
            }

            var metadata = await _storageService.StorePackageAsync(request.Package);
            _logger.LogInformation("Successfully uploaded package {PackageId} {Version}", metadata.Id, metadata.Version);
            
            return new ApiResponse<PackageMetadata>(true, metadata, "Package uploaded successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Package upload failed due to business rule violation");
            return new ApiResponse<PackageMetadata>(false, Message: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload package");
            return new ApiResponse<PackageMetadata>(false, Message: "Failed to upload package");
        }
    }

    public async Task<ApiResponse<PackageListResponse>> SearchPackagesAsync(PackageSearchRequest request)
    {
        try
        {
            var packages = await _storageService.GetPackagesAsync(request.Query, request.Skip, request.Take);
            var totalCount = await _storageService.GetTotalPackageCountAsync(request.Query);
            
            var response = new PackageListResponse(packages, totalCount);
            return new ApiResponse<PackageListResponse>(true, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search packages");
            return new ApiResponse<PackageListResponse>(false, Message: "Failed to search packages");
        }
    }

    public async Task<ApiResponse<PackageMetadata>> GetPackageAsync(string id, string version)
    {
        try
        {
            var package = await _storageService.GetPackageAsync(id, version);
            if (package == null)
            {
                return new ApiResponse<PackageMetadata>(false, Message: "Package not found");
            }

            return new ApiResponse<PackageMetadata>(true, package);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package {PackageId} {Version}", id, version);
            return new ApiResponse<PackageMetadata>(false, Message: "Failed to get package");
        }
    }

    public async Task<ApiResponse<Stream>> DownloadPackageAsync(string id, string version)
    {
        try
        {
            var stream = await _storageService.GetPackageStreamAsync(id, version);
            if (stream == null)
            {
                return new ApiResponse<Stream>(false, Message: "Package not found");
            }

            return new ApiResponse<Stream>(true, stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download package {PackageId} {Version}", id, version);
            return new ApiResponse<Stream>(false, Message: "Failed to download package");
        }
    }

    public async Task<ApiResponse<bool>> DeletePackageAsync(string id, string version)
    {
        try
        {
            var deleted = await _storageService.DeletePackageAsync(id, version);
            if (!deleted)
            {
                return new ApiResponse<bool>(false, Message: "Package not found");
            }

            _logger.LogInformation("Successfully deleted package {PackageId} {Version}", id, version);
            return new ApiResponse<bool>(true, true, "Package deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete package {PackageId} {Version}", id, version);
            return new ApiResponse<bool>(false, Message: "Failed to delete package");
        }
    }
}
