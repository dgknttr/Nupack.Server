extern alias NupackWeb;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using WebModels = NupackWeb::Nupack.Server.Web.Models;
using WebServices = NupackWeb::Nupack.Server.Web.Services;
using WebProgram = NupackWeb::Program;
using Xunit;

namespace Nupack.Server.Tests.Integration;

public class WebSmokeTests
{
    [Fact]
    public async Task HomePage_RendersPackagesAndCanonicalSourceUrl()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Browse Packages");
        html.Should().Contain("TestPackage");
        html.Should().Contain("Stable In View");
        html.Should().Contain("http://localhost:5003/v3/index.json");
        html.Should().NotContain("/Packages/Details");
    }

    [Fact]
    public async Task SearchPage_RendersSeededResults()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Search?q=TestPackage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Search Results");
        html.Should().Contain("TestPackage");
        html.Should().Contain("1 result found");
    }

    [Fact]
    public async Task UploadPage_RendersPublishOnlyAuthGuidance()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Upload");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("self-hosted feed");
        html.Should().Contain("Publish API Key");
        html.Should().Contain("never needs the separate delete credential");
        html.Should().Contain("compatibility-only WriteApiKey");
    }

    [Fact]
    public void Startup_ConfiguresUploadRequestLimitsFromPackageUploadOptions()
    {
        using var factory = CreateFactory(maxPackageSizeBytes: 2 * 1024);

        var formOptions = factory.Services.GetRequiredService<IOptions<FormOptions>>().Value;
        var kestrelOptions = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        formOptions.MultipartBodyLengthLimit.Should().Be(2 * 1024);
        kestrelOptions.Limits.MaxRequestBodySize.Should().Be((2 * 1024) + (1024 * 1024));
    }

    private static WebApplicationFactory<WebProgram> CreateFactory(long? maxPackageSizeBytes = null)
    {
        return new WebApplicationFactory<WebProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Development);
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["PackageUpload:MaxPackageSizeBytes"] = maxPackageSizeBytes?.ToString()
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<WebServices.INuGetApiService>();
                    services.AddSingleton<WebServices.INuGetApiService>(new FakeNuGetApiService());
                });
            });
    }

    private sealed class FakeNuGetApiService : WebServices.INuGetApiService
    {
        private static readonly WebModels.PackageSearchResult Package = new()
        {
            Id = "TestPackage",
            Title = "Test Package",
            Version = "1.0.0",
            Description = "Sample package used for smoke tests.",
            Authors = new List<string> { "Nupack" },
            Tags = new List<string> { "sample", "test" },
            Versions = new List<WebModels.PackageVersion>
            {
                new() { Version = "1.0.0", Id = "/v3/registrations/testpackage/1.0.0.json" }
            }
        };

        public Task<WebModels.ServiceIndex?> GetServiceIndexAsync()
        {
            return Task.FromResult<WebModels.ServiceIndex?>(new WebModels.ServiceIndex
            {
                Version = "3.0.0",
                Resources = new List<WebModels.ServiceResource>
                {
                    new() { Id = "http://localhost:5003/v3/index.json", Type = "ServiceIndex" }
                }
            });
        }

        public Task<WebModels.SearchResponse?> SearchPackagesAsync(string? query = null, int skip = 0, int take = 20, bool includePrerelease = false)
        {
            var matches = string.IsNullOrWhiteSpace(query) || Package.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                ? new List<WebModels.PackageSearchResult> { Package }
                : new List<WebModels.PackageSearchResult>();

            return Task.FromResult<WebModels.SearchResponse?>(new WebModels.SearchResponse
            {
                TotalHits = matches.Count,
                Data = matches.Skip(skip).Take(take).ToList()
            });
        }

        public Task<WebModels.PackageVersionsIndex?> GetPackageVersionsAsync(string packageId)
        {
            return Task.FromResult<WebModels.PackageVersionsIndex?>(new WebModels.PackageVersionsIndex
            {
                Versions = new List<string> { "1.0.0" }
            });
        }

        public Task<WebModels.RegistrationIndex?> GetPackageRegistrationAsync(string packageId)
        {
            return Task.FromResult<WebModels.RegistrationIndex?>(new WebModels.RegistrationIndex
            {
                Id = "/v3/registrations/testpackage/index.json",
                Count = 1,
                Items = new List<WebModels.RegistrationPage>
                {
                    new()
                    {
                        Id = "/v3/registrations/testpackage/page/1.0.0/1.0.0.json",
                        Count = 1,
                        Lower = "1.0.0",
                        Upper = "1.0.0",
                        Items = new List<WebModels.RegistrationLeaf>
                        {
                            new()
                            {
                                Id = "/v3/registrations/testpackage/1.0.0.json",
                                PackageContent = "/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg",
                                Registration = "/v3/registrations/testpackage/index.json",
                                CatalogEntry = new WebModels.CatalogEntry
                                {
                                    IdPackage = "TestPackage",
                                    Version = "1.0.0",
                                    Description = "Sample package used for smoke tests."
                                }
                            }
                        }
                    }
                }
            });
        }

        public async Task<WebModels.RegistrationLeaf?> GetPackageVersionRegistrationAsync(string packageId, string version)
        {
            var registration = await GetPackageRegistrationAsync(packageId);
            return registration?.Items.FirstOrDefault()?.Items.FirstOrDefault();
        }

        public Task<WebModels.HealthStatus?> GetHealthStatusAsync()
        {
            return Task.FromResult<WebModels.HealthStatus?>(new WebModels.HealthStatus
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
