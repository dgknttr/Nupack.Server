using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Nupack.Server.Api.Models;
using Nupack.Server.Api.Models.V3;
using Xunit;

namespace Nupack.Server.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetServiceIndex_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/v3/index.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var serviceIndex = JsonSerializer.Deserialize<ServiceIndex>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        serviceIndex.Should().NotBeNull();
        serviceIndex!.Version.Should().Be("3.0.0");
        serviceIndex.Resources.Should().NotBeEmpty();
        serviceIndex.Resources.Should().Contain(r => r.Type.Contains("SearchQueryService"));
        serviceIndex.Resources.Should().Contain(r => r.Type.Contains("PackageBaseAddress"));
    }

    [Fact]
    public async Task SearchPackages_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/v3/search?q=test&take=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<SearchResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResponse.Should().NotBeNull();
        searchResponse!.TotalHits.Should().BeGreaterOrEqualTo(0);
        searchResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchPackages_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/v3/search?skip=0&take=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<SearchResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResponse.Should().NotBeNull();
        searchResponse!.Data.Should().NotBeNull();
        searchResponse.Data.Count().Should().BeLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetPackageVersions_WithNonExistentPackage_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/v3-flatcontainer/nonexistentpackage/index.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadPackage_WithNonExistentPackage_ReturnsBadRequestOrNotFound()
    {
        // Act
        var response = await _client.GetAsync("/v3-flatcontainer/nonexistentpackage/1.0.0/nonexistentpackage.1.0.0.nupkg");

        // Assert - API validates path parameters first, so could be BadRequest or NotFound
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRegistrationIndex_WithNonExistentPackage_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/v3/registrations/nonexistentpackage/index.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SimpleUI_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/ui");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Nupack Server");
    }

    [Fact]
    public async Task RootPath_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Root path might return JSON (service index) or HTML depending on configuration
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf("text/html", "application/json");

        var content = await response.Content.ReadAsStringAsync();
        // Content should contain either Nupack Server (HTML) or version info (JSON)
        content.Should().MatchRegex("(Nupack|version|3\\.0\\.0)");
    }

    [Fact]
    public async Task Swagger_ReturnsSuccessResponse()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Theory]
    [InlineData("/v3/index.json")]
    [InlineData("/v3/search")]
    [InlineData("/ui")]
    [InlineData("/health")]
    public async Task Endpoints_ReturnSuccessAndCorrectContentType(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().NotBeNull();
    }

    [Fact]
    public async Task V3ApiEndpoints_ReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/v3/index.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
    }

    [Fact]
    public async Task SearchPackages_WithPrerelease_ReturnsCorrectResults()
    {
        // Act
        var response = await _client.GetAsync("/v3/search?prerelease=true&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<SearchResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        searchResponse.Should().NotBeNull();
        searchResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPackageManifest_WithNonExistentPackage_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/v3-flatcontainer/nonexistentpackage/1.0.0/nonexistentpackage.nuspec");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FrontendUI_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/frontend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Nupack");
    }

    [Theory]
    [InlineData("/v3/search?q=test")]
    [InlineData("/v3/search?prerelease=true")]
    [InlineData("/v3/search?skip=0&take=5")]
    [InlineData("/v3/search?q=test&prerelease=true&take=3")]
    public async Task SearchEndpoint_WithDifferentParameters_ReturnsSuccess(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
