using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nupack.Server.Api.Models;

namespace Nupack.Server.Api.Services;

public sealed class HeaderApiKeyPackageEndpointAuthorizer : IPackageEndpointAuthorizer
{
    public const string ApiKeyHeaderName = "X-NuGet-ApiKey";
    public const string InvalidPublishApiKeyMessage = "A valid X-NuGet-ApiKey header is required to publish packages.";
    public const string InvalidDeleteApiKeyMessage = "A valid X-NuGet-ApiKey header is required to delete packages.";
    public const string PublishApiKeyNotConfiguredMessage = "Package publishing requires PackageSecurity:PublishApiKey (or the compatibility-only PackageSecurity:WriteApiKey) outside Development unless PackageSecurity:AllowAnonymousWrites is enabled.";
    public const string DeleteApiKeyNotConfiguredMessage = "Package deletion requires PackageSecurity:DeleteApiKey (or the compatibility-only PackageSecurity:WriteApiKey) outside Development unless PackageSecurity:AllowAnonymousWrites is enabled.";

    private readonly PackageSecurityOptions _options;
    private readonly IWebHostEnvironment _environment;

    public HeaderApiKeyPackageEndpointAuthorizer(IOptions<PackageSecurityOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public Task<PackageAuthorizationResult> AuthorizeUploadAsync(HttpContext httpContext, IFormFile packageFile, CancellationToken cancellationToken = default)
        => Task.FromResult(Authorize(
            httpContext.Request.Headers,
            _options.PublishApiKey,
            InvalidPublishApiKeyMessage,
            PublishApiKeyNotConfiguredMessage));

    public Task<PackageAuthorizationResult> AuthorizeDeleteAsync(HttpContext httpContext, string id, string version, CancellationToken cancellationToken = default)
        => Task.FromResult(Authorize(
            httpContext.Request.Headers,
            _options.DeleteApiKey,
            InvalidDeleteApiKeyMessage,
            DeleteApiKeyNotConfiguredMessage));

    private PackageAuthorizationResult Authorize(
        IHeaderDictionary headers,
        string? operationApiKey,
        string invalidApiKeyMessage,
        string keyNotConfiguredMessage)
    {
        var configuredKey = operationApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            configuredKey = _options.WriteApiKey?.Trim();
        }

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return _environment.IsDevelopment() || _options.AllowAnonymousWrites
                ? PackageAuthorizationResult.Allow()
                : Deny(keyNotConfiguredMessage);
        }

        if (!headers.TryGetValue(ApiKeyHeaderName, out var headerValues) || headerValues.Count != 1)
        {
            return Deny(invalidApiKeyMessage);
        }

        var providedKey = headerValues[0]?.Trim();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Deny(invalidApiKeyMessage);
        }

        return FixedTimeEquals(providedKey, configuredKey)
            ? PackageAuthorizationResult.Allow()
            : Deny(invalidApiKeyMessage);
    }

    private static PackageAuthorizationResult Deny(string message)
        => PackageAuthorizationResult.Deny(StatusCodes.Status401Unauthorized, message);

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
