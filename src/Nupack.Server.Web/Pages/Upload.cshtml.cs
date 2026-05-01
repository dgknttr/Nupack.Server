using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Nupack.Server.Storage;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;

namespace Nupack.Server.Web.Pages;

public class UploadModel : PageModel
{
    private const string ApiKeyHeaderName = "X-NuGet-ApiKey";
    private const string UnauthorizedUploadMessage = "A valid X-NuGet-ApiKey header is required for package write operations.";

    private readonly ILogger<UploadModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PackageUploadOptions _uploadOptions;

    public UploadModel(ILogger<UploadModel> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IOptions<PackageUploadOptions> uploadOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _uploadOptions = uploadOptions.Value;
    }

    [BindProperty]
    public string? ApiKey { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a package file")]
    public IFormFile? PackageFile { get; set; }

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public string MaxPackageSizeDisplay => _uploadOptions.GetMaxPackageSizeDisplay();

    public void OnGet()
    {
        // Initialize page
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (PackageFile == null || PackageFile.Length == 0)
        {
            Message = "Please select a valid package file.";
            IsSuccess = false;
            return Page();
        }

        if (!PackageFile.FileName.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            Message = "Only .nupkg files are allowed.";
            IsSuccess = false;
            return Page();
        }

        if (PackageFile.Length > _uploadOptions.GetResolvedMaxPackageSizeBytes())
        {
            Message = $"Package file size cannot exceed {MaxPackageSizeDisplay}.";
            IsSuccess = false;
            return Page();
        }

        try
        {
            var baseUrl = _configuration.GetValue<string>("NuGetServer:BaseUrl") ?? "http://localhost:5003";

            using var httpClient = _httpClientFactory.CreateClient("NupackUploadClient");
            using var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(PackageFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "package", PackageFile.FileName);

            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, ApiKey);
            }

            var response = await httpClient.PutAsync($"{baseUrl}/v3/push", content);

            if (response.IsSuccessStatusCode)
            {
                Message = $"Package '{PackageFile.FileName}' uploaded successfully!";
                IsSuccess = true;
                _logger.LogInformation("Package uploaded successfully: {FileName}", PackageFile.FileName);

                ApiKey = null;
                PackageFile = null;
                ModelState.Clear();
            }
            else
            {
                Message = await GetErrorMessageAsync(response);
                IsSuccess = false;
                _logger.LogWarning("Package upload failed: {StatusCode} - {Error}", response.StatusCode, Message);
            }
        }
        catch (Exception ex)
        {
            Message = "An error occurred during upload. Please try again.";
            IsSuccess = false;
            _logger.LogError(ex, "Error uploading package: {FileName}", PackageFile?.FileName);
        }

        return Page();
    }

    private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        ProblemDetails? problemDetails = null;

        try
        {
            problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch
        {
            // Ignore malformed error payloads and fall back to raw content.
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return problemDetails?.Detail ?? UnauthorizedUploadMessage;
        }

        if (!string.IsNullOrWhiteSpace(problemDetails?.Detail))
        {
            return $"Upload failed: {problemDetails.Detail}";
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        return $"Upload failed: {response.StatusCode} - {errorContent}";
    }
}
