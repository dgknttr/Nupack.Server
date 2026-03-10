using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nupack.Server.Api.Models.V3;
using Nupack.Server.Api.Services;
using Nupack.Server.Tests.Storage;
using Xunit;

namespace Nupack.Server.Tests.Integration;

public class S3ProtocolIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task SeededBucket_ExposesPackageThroughCoreV3Endpoints()
    {
        using var server = await S3TestServerContext.CreateAsync(seedPackage: true);
        if (!server.IsAvailable)
        {
            return;
        }

        var client = server.Client!;
        var versionsResponse = await client.GetAsync("/v3-flatcontainer/testpackage/index.json");
        var searchResponse = await client.GetAsync("/v3/search?q=TestPackage&take=5");
        var registrationResponse = await client.GetAsync("/v3/registrations/testpackage/index.json");

        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var versions = await ReadJsonAsync<PackageVersionsIndex>(versionsResponse);
        versions.Versions.Should().Contain("1.0.0");
    }

    [Fact]
    public async Task PushAndDelete_WithWriteApiKeyConfigured_WorkAgainstS3Storage()
    {
        using var server = await S3TestServerContext.CreateAsync(seedPackage: false, writeApiKey: "secret-key");
        if (!server.IsAvailable)
        {
            return;
        }

        var client = server.Client!;
        client.DefaultRequestHeaders.Add(HeaderApiKeyPackageEndpointAuthorizer.ApiKeyHeaderName, "secret-key");
        using var content = await CreatePackageUploadContentAsync(server.SamplePackagePath);

        var uploadResponse = await client.PutAsync("/v3/push", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionsAfterUpload = await client.GetAsync("/v3-flatcontainer/testpackage/index.json");
        versionsAfterUpload.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync("/v3/delete/TestPackage/1.0.0");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    private sealed class S3TestServerContext : IDisposable
    {
        private readonly S3TestEnvironment? _environment;
        private readonly Amazon.S3.IAmazonS3? _client;
        private readonly string? _bucketName;

        private S3TestServerContext(bool isAvailable, string samplePackagePath, HttpClient? httpClient, WebApplicationFactory<Program>? factory, S3TestEnvironment? environment, Amazon.S3.IAmazonS3? client, string? bucketName)
        {
            IsAvailable = isAvailable;
            SamplePackagePath = samplePackagePath;
            Client = httpClient;
            Factory = factory;
            _environment = environment;
            _client = client;
            _bucketName = bucketName;
        }

        public bool IsAvailable { get; }
        public string SamplePackagePath { get; }
        public HttpClient? Client { get; }
        public WebApplicationFactory<Program>? Factory { get; }

        public static async Task<S3TestServerContext> CreateAsync(bool seedPackage, string? writeApiKey = null)
        {
            var environment = S3TestEnvironment.TryCreate();
            var samplePackagePath = TestPackageLocator.ResolveSamplePackagePath();
            if (environment is null)
            {
                return new S3TestServerContext(false, samplePackagePath, null, null, null, null, null);
            }

            var bucketName = $"nupack-api-{Guid.NewGuid():N}";
            var client = environment.CreateClient();
            await environment.EnsureBucketExistsAsync(client, bucketName);

            if (seedPackage)
            {
                var key = Nupack.Server.Storage.S3.S3PackageObjectKey.Build(environment.Prefix ?? string.Empty, "TestPackage", "1.0.0");
                await using var packageStream = File.OpenRead(samplePackagePath);
                await client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = packageStream,
                    AutoCloseStream = false,
                    ContentType = "application/octet-stream"
                });

                await environment.WaitForObjectVisibilityAsync(client, bucketName, key);
            }

            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment(Environments.Development);
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["PackageStorage:Provider"] = "S3",
                            ["PackageStorage:S3:BucketName"] = bucketName,
                            ["PackageStorage:S3:Region"] = environment.Region,
                            ["PackageStorage:S3:ServiceUrl"] = environment.ServiceUrl,
                            ["PackageStorage:S3:AccessKey"] = environment.AccessKey,
                            ["PackageStorage:S3:SecretKey"] = environment.SecretKey,
                            ["PackageStorage:S3:ForcePathStyle"] = environment.ForcePathStyle.ToString(),
                            ["PackageStorage:S3:Prefix"] = environment.Prefix,
                            ["PackageSecurity:WriteApiKey"] = writeApiKey
                        });
                    });
                });

            return new S3TestServerContext(true, samplePackagePath, factory.CreateClient(), factory, environment, client, bucketName);
        }

        public void Dispose()
        {
            Client?.Dispose();
            Factory?.Dispose();

            if (_environment is not null && _client is not null && !string.IsNullOrWhiteSpace(_bucketName))
            {
                _environment.DeleteBucketRecursiveAsync(_client, _bucketName).GetAwaiter().GetResult();
                _client.Dispose();
            }
        }
    }
}
