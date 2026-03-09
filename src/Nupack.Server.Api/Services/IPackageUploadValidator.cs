using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public interface IPackageUploadValidator
{
    Task<PackageUploadValidationResult> ValidateAsync(PackageUploadRequest request, CancellationToken cancellationToken = default);
}

public sealed record PackageUploadValidationResult(bool IsValid, string? Message = null)
{
    public static PackageUploadValidationResult Success() => new(true);

    public static PackageUploadValidationResult Fail(string message) => new(false, message);
}

public sealed class DefaultPackageUploadValidator : IPackageUploadValidator
{
    public Task<PackageUploadValidationResult> ValidateAsync(PackageUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Package == null || request.Package.Length == 0)
        {
            return Task.FromResult(PackageUploadValidationResult.Fail("Package file is required"));
        }

        if (!Path.GetExtension(request.Package.FileName).Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(PackageUploadValidationResult.Fail("Only .nupkg files are supported"));
        }

        return Task.FromResult(PackageUploadValidationResult.Success());
    }
}
