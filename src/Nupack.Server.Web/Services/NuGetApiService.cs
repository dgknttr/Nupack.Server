using System.Text.Json;
using Nupack.Server.Web.Models;

namespace Nupack.Server.Web.Services;

public class NuGetApiService : INuGetApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NuGetApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public NuGetApiService(HttpClient httpClient, ILogger<NuGetApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ServiceIndex?> GetServiceIndexAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v3/index.json");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ServiceIndex>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service index");
        }
        return null;
    }

    public async Task<SearchResponse?> SearchPackagesAsync(string? query = null, int skip = 0, int take = 20, bool includePrerelease = false)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"skip={skip}",
                $"take={take}",
                $"prerelease={includePrerelease.ToString().ToLower()}"
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryParams.Add($"q={Uri.EscapeDataString(query)}");
            }

            var url = $"/v3/search?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SearchResponse>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search packages with query: {Query}", query);
        }
        return null;
    }

    public async Task<PackageVersionsIndex?> GetPackageVersionsAsync(string packageId)
    {
        try
        {
            var url = $"/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageVersionsIndex>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package versions for {PackageId}", packageId);
        }
        return null;
    }

    public async Task<RegistrationIndex?> GetPackageRegistrationAsync(string packageId)
    {
        try
        {
            var url = $"/v3/registrations/{packageId.ToLowerInvariant()}/index.json";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RegistrationIndex>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package registration for {PackageId}", packageId);
        }
        return null;
    }

    public async Task<RegistrationLeaf?> GetPackageVersionRegistrationAsync(string packageId, string version)
    {
        try
        {
            var url = $"/v3/registrations/{packageId.ToLowerInvariant()}/{version.ToLowerInvariant()}.json";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RegistrationLeaf>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package version registration for {PackageId} {Version}", packageId, version);
        }
        return null;
    }

    public async Task<HealthStatus?> GetHealthStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HealthStatus>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
        }
        return new HealthStatus { Status = "unhealthy", Timestamp = DateTime.UtcNow };
    }
}
