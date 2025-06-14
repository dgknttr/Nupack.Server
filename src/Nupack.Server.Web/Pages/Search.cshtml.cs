using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Nupack.Server.Web.Models;
using Nupack.Server.Web.Services;

namespace Nupack.Server.Web.Pages;

public class SearchModel : PageModel
{
    private readonly INuGetApiService _nugetApiService;
    private readonly ILogger<SearchModel> _logger;
    private readonly BrandingOptions _brandingOptions;

    public SearchModel(INuGetApiService nugetApiService, ILogger<SearchModel> logger, IOptions<BrandingOptions> brandingOptions)
    {
        _nugetApiService = nugetApiService;
        _logger = logger;
        _brandingOptions = brandingOptions.Value;
    }

    public BrandingOptions BrandingOptions => _brandingOptions;

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludePrerelease { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public List<PackageSearchResult> Packages { get; set; } = new();
    public int TotalHits { get; set; }
    public int CurrentPage => Page;
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }

    private const int PackagesPerPage = 20;

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }

        try
        {
            var skip = (Page - 1) * PackagesPerPage;
            var searchResponse = await _nugetApiService.SearchPackagesAsync(
                query: Query.Trim(),
                skip: skip,
                take: PackagesPerPage,
                includePrerelease: IncludePrerelease
            );

            if (searchResponse != null)
            {
                Packages = searchResponse.Data;
                TotalHits = searchResponse.TotalHits;
                TotalPages = (int)Math.Ceiling((double)TotalHits / PackagesPerPage);
            }
            else
            {
                ErrorMessage = "Unable to search packages. Please try again.";
                _logger.LogWarning("Failed to search packages with query: {Query}", Query);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while searching packages.";
            _logger.LogError(ex, "Error searching packages with query: {Query}", Query);
        }
    }
}
