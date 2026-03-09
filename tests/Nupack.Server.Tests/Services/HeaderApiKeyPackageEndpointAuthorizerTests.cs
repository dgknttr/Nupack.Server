using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class HeaderApiKeyPackageEndpointAuthorizerTests
{
    [Fact]
    public async Task AuthorizeUploadAsync_WithNoConfiguredKey_AllowsRequest()
    {
        var authorizer = CreateAuthorizer(writeApiKey: null);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMatchingKey_AllowsRequest()
    {
        var authorizer = CreateAuthorizer("secret-key");
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = " secret-key ";

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithWrongKey_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key");
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = "wrong-key";

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMissingHeader_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key");
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMultipleHeaderValues_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key");
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "first");
        context.Request.Headers.Append(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "second");

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeDeleteAsync_WithMatchingKey_AllowsRequest()
    {
        var authorizer = CreateAuthorizer("secret-key");
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = "secret-key";

        var result = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        result.IsAllowed.Should().BeTrue();
    }

    private static HeaderApiKeyPackageEndpointAuthorizer CreateAuthorizer(string? writeApiKey)
    {
        return new HeaderApiKeyPackageEndpointAuthorizer(Options.Create(new PackageSecurityOptions
        {
            WriteApiKey = writeApiKey
        }));
    }

    private static IFormFile CreatePackageFile()
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "package", "TestPackage.1.0.0.nupkg");
    }
}
