using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public sealed class HeaderApiKeyPackageEndpointAuthorizer : IPackageEndpointAuthorizer
{
    public const string ApiKeyHeaderName = "X-NuGet-ApiKey";
    public const string InvalidApiKeyMessage = "A valid X-NuGet-ApiKey header is required for package write operations.";

    private readonly PackageSecurityOptions _options;

    public HeaderApiKeyPackageEndpointAuthorizer(IOptions<PackageSecurityOptions> options)
    {
        _options = options.Value;
    }

    public Task<PackageAuthorizationResult> AuthorizeUploadAsync(HttpContext httpContext, IFormFile packageFile, CancellationToken cancellationToken = default)
        => Task.FromResult(Authorize(httpContext.Request.Headers));

    public Task<PackageAuthorizationResult> AuthorizeDeleteAsync(HttpContext httpContext, string id, string version, CancellationToken cancellationToken = default)
        => Task.FromResult(Authorize(httpContext.Request.Headers));

    private PackageAuthorizationResult Authorize(IHeaderDictionary headers)
    {
        var configuredKey = _options.WriteApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return PackageAuthorizationResult.Allow();
        }

        if (!headers.TryGetValue(ApiKeyHeaderName, out var headerValues) || headerValues.Count != 1)
        {
            return Deny();
        }

        var providedKey = headerValues[0]?.Trim();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Deny();
        }

        return FixedTimeEquals(providedKey, configuredKey)
            ? PackageAuthorizationResult.Allow()
            : Deny();
    }

    private static PackageAuthorizationResult Deny()
        => PackageAuthorizationResult.Deny(StatusCodes.Status401Unauthorized, InvalidApiKeyMessage);

    private static bool FixedTimeEquals(string providedKey, string configuredKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);

        if (providedBytes.Length != configuredBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
