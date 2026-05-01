using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Services;
using Xunit;

namespace Nupack.Server.Tests.Services;

public class HeaderApiKeyPackageEndpointAuthorizerTests
{
    [Fact]
    public async Task AuthorizeUploadAsync_WithNoConfiguredKeyInDevelopment_AllowsRequest()
    {
        var authorizer = CreateAuthorizer(writeApiKey: null, environmentName: Environments.Development);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithNoConfiguredKeyInProduction_DeniesRequest()
    {
        var authorizer = CreateAuthorizer(writeApiKey: null, environmentName: Environments.Production);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.AnonymousWritesDisabledMessage);
    }

    [Fact]
    public async Task AuthorizeDeleteAsync_WithNoConfiguredKeyInProduction_DeniesRequest()
    {
        var authorizer = CreateAuthorizer(writeApiKey: null, environmentName: Environments.Production);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.AnonymousWritesDisabledMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithNoConfiguredKeyAndAnonymousWritesAllowedInProduction_AllowsRequest()
    {
        var authorizer = CreateAuthorizer(
            writeApiKey: null,
            environmentName: Environments.Production,
            allowAnonymousWrites: true);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMatchingKey_AllowsRequest()
    {
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = " secret-key ";

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithWrongKey_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
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
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithConfiguredKeyAndAnonymousWritesAllowed_DeniesMissingHeader()
    {
        var authorizer = CreateAuthorizer(
            "secret-key",
            Environments.Production,
            allowAnonymousWrites: true);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMultipleHeaderValues_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
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
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = "secret-key";

        var result = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        result.IsAllowed.Should().BeTrue();
    }

    private static HeaderApiKeyPackageEndpointAuthorizer CreateAuthorizer(
        string? writeApiKey,
        string environmentName = "Development",
        bool allowAnonymousWrites = false)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        return new HeaderApiKeyPackageEndpointAuthorizer(Options.Create(new PackageSecurityOptions
        {
            WriteApiKey = writeApiKey,
            AllowAnonymousWrites = allowAnonymousWrites
        }), environment.Object);
    }

    private static IFormFile CreatePackageFile()
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "package", "TestPackage.1.0.0.nupkg");
    }
}
