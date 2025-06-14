using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Nupack.Server.Web.Models;
using Nupack.Server.Web.Services;

namespace Nupack.Server.Web.Pages;

public class IndexModel : PageModel
{
    private readonly INuGetApiService _nugetApiService;
    private readonly ILogger<IndexModel> _logger;
    private readonly BrandingOptions _brandingOptions;
    private readonly IConfiguration _configuration;

    public IndexModel(INuGetApiService nugetApiService, ILogger<IndexModel> logger, IOptions<BrandingOptions> brandingOptions, IConfiguration configuration)
    {
        _nugetApiService = nugetApiService;
        _logger = logger;
        _brandingOptions = brandingOptions.Value;
        _configuration = configuration;
    }

    public BrandingOptions BrandingOptions => _brandingOptions;
    public string NugetSourceUrl => GetNugetSourceUrl();

    public List<PackageSearchResult> Packages { get; set; } = new();
    public int TotalPackages { get; set; }
    public int StablePackages { get; set; }
    public int PrereleasePackages { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Load recent packages for the home page
            var searchResponse = await _nugetApiService.SearchPackagesAsync(
                query: null, 
                skip: 0, 
                take: 12, 
                includePrerelease: true
            );

            if (searchResponse != null)
            {
                Packages = searchResponse.Data;
                TotalPackages = searchResponse.TotalHits;
                
                // Calculate stats
                StablePackages = Packages.Count(p => !p.IsPrerelease);
                PrereleasePackages = Packages.Count(p => p.IsPrerelease);
            }
            else
            {
                ErrorMessage = "Unable to load packages from the NuGet server.";
                _logger.LogWarning("Failed to load packages for home page");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while loading packages.";
            _logger.LogError(ex, "Error loading packages for home page");
        }
    }

    private string GetNugetSourceUrl()
    {
        // First try to get from Branding configuration
        var brandingUrl = _configuration.GetValue<string>("Branding:NugetSourceUrl");
        if (!string.IsNullOrWhiteSpace(brandingUrl))
        {
            return brandingUrl;
        }

        // Fallback to NuGetServer BaseUrl + /v3/index.json
        var baseUrl = _configuration.GetValue<string>("NuGetServer:BaseUrl") ?? "http://localhost:5003";
        return $"{baseUrl.TrimEnd('/')}/v3/index.json";
    }
}
