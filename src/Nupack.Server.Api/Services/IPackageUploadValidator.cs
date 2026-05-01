using Microsoft.Extensions.Options;
using Nupack.Server.Api.Models;
using Nupack.Server.Storage;

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
    private readonly PackageUploadOptions _options;

    public DefaultPackageUploadValidator(IOptions<PackageUploadOptions> options)
    {
        _options = options.Value;
    }

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

        var maxPackageSizeBytes = _options.GetResolvedMaxPackageSizeBytes();
        if (request.Package.Length > maxPackageSizeBytes)
        {
            return Task.FromResult(PackageUploadValidationResult.Fail($"Package file size cannot exceed {_options.GetMaxPackageSizeDisplay()}."));
        }

        return Task.FromResult(PackageUploadValidationResult.Success());
    }
}
