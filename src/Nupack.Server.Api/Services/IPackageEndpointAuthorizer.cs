namespace Nupack.Server.Api.Services;

public interface IPackageEndpointAuthorizer
{
    Task<PackageAuthorizationResult> AuthorizeUploadAsync(HttpContext httpContext, IFormFile packageFile, CancellationToken cancellationToken = default);
    Task<PackageAuthorizationResult> AuthorizeDeleteAsync(HttpContext httpContext, string id, string version, CancellationToken cancellationToken = default);
}

public sealed record PackageAuthorizationResult(bool IsAllowed, int StatusCode = StatusCodes.Status200OK, string? Message = null)
{
    public static PackageAuthorizationResult Allow() => new(true);

    public static PackageAuthorizationResult Deny(int statusCode = StatusCodes.Status403Forbidden, string? message = null)
        => new(false, statusCode, message ?? "Package operation rejected.");
}

public sealed class AllowAnonymousPackageEndpointAuthorizer : IPackageEndpointAuthorizer
{
    public Task<PackageAuthorizationResult> AuthorizeUploadAsync(HttpContext httpContext, IFormFile packageFile, CancellationToken cancellationToken = default)
        => Task.FromResult(PackageAuthorizationResult.Allow());

    public Task<PackageAuthorizationResult> AuthorizeDeleteAsync(HttpContext httpContext, string id, string version, CancellationToken cancellationToken = default)
        => Task.FromResult(PackageAuthorizationResult.Allow());
}
