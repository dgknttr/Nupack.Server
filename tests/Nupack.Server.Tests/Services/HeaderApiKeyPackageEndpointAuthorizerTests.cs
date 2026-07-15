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
    public async Task PublishKey_AuthorizesUpload_ButNotDelete()
    {
        var authorizer = CreateAuthorizer(
            publishApiKey: "publish-key",
            environmentName: Environments.Production);
        var context = CreateContextWithApiKey("publish-key");

        var uploadResult = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());
        var deleteResult = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        uploadResult.IsAllowed.Should().BeTrue();
        deleteResult.IsAllowed.Should().BeFalse();
        deleteResult.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.DeleteApiKeyNotConfiguredMessage);
    }

    [Fact]
    public async Task DeleteKey_AuthorizesDelete_ButNotUpload()
    {
        var authorizer = CreateAuthorizer(
            deleteApiKey: "delete-key",
            environmentName: Environments.Production);
        var context = CreateContextWithApiKey("delete-key");

        var deleteResult = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");
        var uploadResult = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        deleteResult.IsAllowed.Should().BeTrue();
        uploadResult.IsAllowed.Should().BeFalse();
        uploadResult.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.PublishApiKeyNotConfiguredMessage);
    }

    [Fact]
    public async Task LegacyWriteKey_AuthorizesBothOperations()
    {
        var authorizer = CreateAuthorizer(
            writeApiKey: "legacy-key",
            environmentName: Environments.Production);
        var context = CreateContextWithApiKey("legacy-key");

        var uploadResult = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());
        var deleteResult = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        uploadResult.IsAllowed.Should().BeTrue();
        deleteResult.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task PublishKey_TakesPrecedenceOverLegacyWriteKey()
    {
        var authorizer = CreateAuthorizer(
            writeApiKey: "legacy-key",
            publishApiKey: "publish-key",
            environmentName: Environments.Production);

        var publishResult = await authorizer.AuthorizeUploadAsync(
            CreateContextWithApiKey("publish-key"),
            CreatePackageFile());
        var legacyResult = await authorizer.AuthorizeUploadAsync(
            CreateContextWithApiKey("legacy-key"),
            CreatePackageFile());

        publishResult.IsAllowed.Should().BeTrue();
        legacyResult.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteKey_TakesPrecedenceOverLegacyWriteKey()
    {
        var authorizer = CreateAuthorizer(
            writeApiKey: "legacy-key",
            deleteApiKey: "delete-key",
            environmentName: Environments.Production);

        var deleteResult = await authorizer.AuthorizeDeleteAsync(
            CreateContextWithApiKey("delete-key"),
            "TestPackage",
            "1.0.0");
        var legacyResult = await authorizer.AuthorizeDeleteAsync(
            CreateContextWithApiKey("legacy-key"),
            "TestPackage",
            "1.0.0");

        deleteResult.IsAllowed.Should().BeTrue();
        legacyResult.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task WhitespaceOperationKeys_FallBackToLegacyWriteKey()
    {
        var authorizer = CreateAuthorizer(
            writeApiKey: "legacy-key",
            publishApiKey: "  ",
            deleteApiKey: "\t",
            environmentName: Environments.Production);
        var context = CreateContextWithApiKey("legacy-key");

        var uploadResult = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());
        var deleteResult = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        uploadResult.IsAllowed.Should().BeTrue();
        deleteResult.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task MissingDeleteKey_FailsClosedOutsideDevelopment()
    {
        var authorizer = CreateAuthorizer(
            publishApiKey: "publish-key",
            environmentName: Environments.Production);

        var result = await authorizer.AuthorizeDeleteAsync(new DefaultHttpContext(), "TestPackage", "1.0.0");

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.DeleteApiKeyNotConfiguredMessage);
    }

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
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.PublishApiKeyNotConfiguredMessage);
    }

    [Fact]
    public async Task AuthorizeDeleteAsync_WithNoConfiguredKeyInProduction_DeniesRequest()
    {
        var authorizer = CreateAuthorizer(writeApiKey: null, environmentName: Environments.Production);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeDeleteAsync(context, "TestPackage", "1.0.0");

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.DeleteApiKeyNotConfiguredMessage);
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
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
    }

    [Fact]
    public async Task AuthorizeUploadAsync_WithMissingHeader_DeniesRequest()
    {
        var authorizer = CreateAuthorizer("secret-key", Environments.Production);
        var context = new DefaultHttpContext();

        var result = await authorizer.AuthorizeUploadAsync(context, CreatePackageFile());

        result.IsAllowed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
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
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
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
        result.Message.Should().Be(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
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
        string? writeApiKey = null,
        string environmentName = "Development",
        bool allowAnonymousWrites = false,
        string? publishApiKey = null,
        string? deleteApiKey = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        return new HeaderApiKeyPackageEndpointAuthorizer(Options.Create(new PackageSecurityOptions
        {
            WriteApiKey = writeApiKey,
            PublishApiKey = publishApiKey,
            DeleteApiKey = deleteApiKey,
            AllowAnonymousWrites = allowAnonymousWrites
        }), environment.Object);
    }

    private static DefaultHttpContext CreateContextWithApiKey(string apiKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName] = apiKey;
        return context;
    }

    private static IFormFile CreatePackageFile()
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "package", "TestPackage.1.0.0.nupkg");
    }
}
