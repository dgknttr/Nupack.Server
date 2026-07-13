using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nupack.Server.Api.Models.V3;
using Nupack.Server.Api.Services;
using Xunit;

namespace Nupack.Server.Tests.Integration;

public class ProtocolIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetServiceIndex_WithSeededPackage_ReturnsExpectedResources()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3/index.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var serviceIndex = await ReadJsonAsync<ServiceIndex>(response);

        serviceIndex.Version.Should().Be("3.0.0");
        serviceIndex.Resources.Should().Contain(resource => resource.Type.Contains("SearchQueryService"));
        serviceIndex.Resources.Should().Contain(resource => resource.Type.Contains("PackageBaseAddress"));
        serviceIndex.Resources.Should().Contain(resource => resource.Type == "PackagePublish/2.0.0" && resource.Id.EndsWith("/v3/push"));
        serviceIndex.Resources.Should().OnlyContain(resource => resource.Id.StartsWith("http://localhost/"));
    }

    [Fact]
    public async Task GetPackageVersions_WithSeededPackage_ReturnsPublishedVersion()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3-flatcontainer/testpackage/index.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await ReadJsonAsync<PackageVersionsIndex>(response);
        versions.Versions.Should().ContainSingle().Which.Should().Be("1.0.0");
    }

    [Fact]
    public async Task DownloadPackage_WithSeededPackage_ReturnsPackageStream()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/octet-stream");
        var downloadedFileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;
        downloadedFileName.Should().MatchRegex("(?i)^testpackage\\.1\\.0\\.0\\.nupkg$");

        var packageBytes = await response.Content.ReadAsByteArrayAsync();
        packageBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPackageManifest_WithSeededPackage_ReturnsNuspecXml()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3-flatcontainer/testpackage/1.0.0/testpackage.nuspec");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/xml");

        var manifest = XDocument.Parse(await response.Content.ReadAsStringAsync());
        manifest.Root?.Name.LocalName.Should().Be("package");
        manifest.Descendants().Should().Contain(element => element.Name.LocalName == "id" && element.Value == "TestPackage");
        manifest.Descendants().Should().Contain(element => element.Name.LocalName == "version" && element.Value == "1.0.0");
    }

    [Fact]
    public async Task SearchPackages_WithSeededPackage_ReturnsPublishedPackage()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3/search?q=TestPackage&take=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResponse = await ReadJsonAsync<SearchResponse>(response);

        searchResponse.TotalHits.Should().BeGreaterThan(0);
        searchResponse.Data.Should().Contain(result => result.PackageId == "TestPackage" && result.Version == "1.0.0");
    }

    [Fact]
    public async Task GetRegistrationIndex_WithSeededPackage_ReturnsPackageMetadata()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3/registrations/testpackage/index.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var registration = await ReadJsonAsync<RegistrationIndex>(response);

        registration.Count.Should().Be(1);
        registration.Items.Should().ContainSingle();
        registration.Items[0].Lower.Should().Be("1.0.0");
        registration.Items[0].Upper.Should().Be("1.0.0");
        registration.Items[0].Items.Should().ContainSingle(item => item.CatalogEntry.Id_Package == "TestPackage");
    }

    [Fact]
    public async Task GetRegistrationPage_WithSeededPackage_ReturnsLeafWithinRange()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3/registrations/testpackage/page/1.0.0/1.0.0.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var registrationPage = await ReadJsonAsync<RegistrationPage>(response);

        registrationPage.Count.Should().Be(1);
        registrationPage.Items.Should().ContainSingle(item => item.CatalogEntry.Version == "1.0.0");
    }

    [Fact]
    public async Task GetRegistrationLeaf_WithSeededPackage_ReturnsLeafMetadata()
    {
        using var server = new TestServerContext(seedPackage: true);

        var response = await server.Client.GetAsync("/v3/registrations/testpackage/1.0.0.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var registrationLeaf = await ReadJsonAsync<RegistrationLeaf>(response);

        registrationLeaf.CatalogEntry.Id_Package.Should().Be("TestPackage");
        registrationLeaf.CatalogEntry.Version.Should().Be("1.0.0");
        registrationLeaf.PackageContent.Should().Contain("/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyPayload()
    {
        using var server = new TestServerContext(seedPackage: false);

        var response = await server.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("healthy");
        payload.Should().Contain("timestamp");
    }

    [Fact]
    public async Task ReadEndpoints_WithWriteApiKeyConfigured_RemainAccessible()
    {
        using var server = new TestServerContext(seedPackage: true, writeApiKey: "secret-key");

        var searchResponse = await server.Client.GetAsync("/v3/search?q=TestPackage&take=5");
        var downloadResponse = await server.Client.GetAsync("/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg");
        var registrationResponse = await server.Client.GetAsync("/v3/registrations/testpackage/index.json");
        var healthResponse = await server.Client.GetAsync("/health");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPackage_WithWriteApiKeyConfiguredAndMissingHeader_ReturnsUnauthorized()
    {
        using var server = new TestServerContext(seedPackage: false, writeApiKey: "secret-key");
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
    }

    [Fact]
    public async Task UploadPackage_WithWriteApiKeyConfiguredAndWrongHeader_ReturnsUnauthorized()
    {
        using var server = new TestServerContext(seedPackage: false, writeApiKey: "secret-key");
        server.Client.DefaultRequestHeaders.Add(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "wrong-key");
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(HeaderApiKeyPackageEndpointAuthorizer.InvalidPublishApiKeyMessage);
    }

    [Fact]
    public async Task UploadPackage_WithCustomAuthorizerDenied_ReturnsForbidden()
    {
        using var server = new TestServerContext(seedPackage: false, configureServices: services =>
        {
            services.AddScoped<IPackageEndpointAuthorizer>(_ => new DenyPackageEndpointAuthorizer("Uploads require custom auth."));
        });
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Uploads require custom auth.");
    }

    [Fact]
    public async Task UploadPackage_WithValidPackage_ReturnsCreatedAndMakesPackageAvailable()
    {
        using var server = new TestServerContext(seedPackage: false);
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var uploadResponse = await server.Client.PutAsync("/v3/push", content);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionsResponse = await server.Client.GetAsync("/v3-flatcontainer/testpackage/index.json");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var versions = await ReadJsonAsync<PackageVersionsIndex>(versionsResponse);
        versions.Versions.Should().Contain("1.0.0");
    }

    [Fact]
    public async Task UploadPackage_WithWriteApiKeyConfiguredAndCorrectHeader_ReturnsCreatedAndMakesPackageAvailable()
    {
        using var server = new TestServerContext(seedPackage: false, writeApiKey: "secret-key");
        server.Client.DefaultRequestHeaders.Add(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "secret-key");
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionsResponse = await server.Client.GetAsync("/v3-flatcontainer/testpackage/index.json");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPackage_WithDuplicatePackage_ReturnsBadRequest()
    {
        using var server = new TestServerContext(seedPackage: true);
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("already exists");
    }

    [Fact]
    public async Task UploadPackage_WithPackageAboveConfiguredLimit_ReturnsBadRequest()
    {
        using var server = new TestServerContext(seedPackage: false, maxPackageSizeBytes: 3);
        using var content = CreatePackageUploadContent(new byte[] { 1, 2, 3, 4 }, "TestPackage.1.0.0.nupkg");

        var response = await server.Client.PutAsync("/v3/push", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Package file size cannot exceed 3 bytes.");
    }

    [Fact]
    public async Task DeletePackage_WithWriteApiKeyConfiguredAndMissingHeader_ReturnsUnauthorized()
    {
        using var server = new TestServerContext(seedPackage: true, writeApiKey: "secret-key");

        var response = await server.Client.DeleteAsync("/v3/delete/TestPackage/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(HeaderApiKeyPackageEndpointAuthorizer.InvalidDeleteApiKeyMessage);
    }

    [Fact]
    public async Task DeletePackage_WithCustomAuthorizerDenied_ReturnsForbidden()
    {
        using var server = new TestServerContext(seedPackage: true, configureServices: services =>
        {
            services.AddScoped<IPackageEndpointAuthorizer>(_ => new DenyPackageEndpointAuthorizer("Deletes require custom auth."));
        });

        var response = await server.Client.DeleteAsync("/v3/delete/TestPackage/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Deletes require custom auth.");
    }

    [Fact]
    public async Task DeletePackage_WithSeededPackage_ReturnsNoContentAndRemovesPackage()
    {
        using var server = new TestServerContext(seedPackage: true);

        var deleteResponse = await server.Client.DeleteAsync("/v3/delete/TestPackage/1.0.0");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var versionsResponse = await server.Client.GetAsync("/v3-flatcontainer/testpackage/index.json");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePackage_WithWriteApiKeyConfiguredAndCorrectHeader_ReturnsNoContent()
    {
        using var server = new TestServerContext(seedPackage: true, writeApiKey: "secret-key");
        server.Client.DefaultRequestHeaders.Add(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "secret-key");

        var response = await server.Client.DeleteAsync("/v3/delete/TestPackage/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePackage_WithMissingPackage_ReturnsNotFound()
    {
        using var server = new TestServerContext(seedPackage: false);

        var response = await server.Client.DeleteAsync("/v3/delete/TestPackage/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        result.Should().NotBeNull();
        return result!;
    }

    private static async Task<MultipartFormDataContent> CreatePackageUploadContentAsync(string packagePath)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(packagePath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "package", Path.GetFileName(packagePath));
        return content;
    }

    private static MultipartFormDataContent CreatePackageUploadContent(byte[] bytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "package", fileName);
        return content;
    }

    private sealed class TestServerContext : IDisposable
    {
        private readonly string _storagePath;

        public TestServerContext(bool seedPackage, string? writeApiKey = null, Action<IServiceCollection>? configureServices = null, long? maxPackageSizeBytes = null)
        {
            _storagePath = Path.Combine(Path.GetTempPath(), "Nupack.Server.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_storagePath);

            SamplePackagePath = ResolveSamplePackagePath();
            if (seedPackage)
            {
                File.Copy(SamplePackagePath, Path.Combine(_storagePath, Path.GetFileName(SamplePackagePath)));
            }

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment(Environments.Development);
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["PackageStorage:BasePath"] = _storagePath,
                            ["PackageSecurity:WriteApiKey"] = writeApiKey,
                            ["PackageUpload:MaxPackageSizeBytes"] = maxPackageSizeBytes?.ToString()
                        });
                    });
                    builder.ConfigureServices(services => configureServices?.Invoke(services));
                });

            Client = Factory.CreateClient();
        }

        public string SamplePackagePath { get; }
        public WebApplicationFactory<Program> Factory { get; }
        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();

            if (Directory.Exists(_storagePath))
            {
                Directory.Delete(_storagePath, recursive: true);
            }
        }

        private static string ResolveSamplePackagePath()
        {
            var current = AppContext.BaseDirectory;

            for (var depth = 0; depth < 8; depth++)
            {
                var candidate = Path.Combine(current, "test", "TestPackage.1.0.0.nupkg");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            throw new FileNotFoundException("Could not locate TestPackage.1.0.0.nupkg for integration tests.");
        }
    }

    private sealed class DenyPackageEndpointAuthorizer : IPackageEndpointAuthorizer
    {
        private readonly string _message;

        public DenyPackageEndpointAuthorizer(string message)
        {
            _message = message;
        }

        public Task<PackageAuthorizationResult> AuthorizeUploadAsync(HttpContext httpContext, IFormFile packageFile, CancellationToken cancellationToken = default)
            => Task.FromResult(PackageAuthorizationResult.Deny(StatusCodes.Status403Forbidden, _message));

        public Task<PackageAuthorizationResult> AuthorizeDeleteAsync(HttpContext httpContext, string id, string version, CancellationToken cancellationToken = default)
            => Task.FromResult(PackageAuthorizationResult.Deny(StatusCodes.Status403Forbidden, _message));
    }
}
