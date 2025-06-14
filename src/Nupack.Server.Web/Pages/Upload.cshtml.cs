using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Nupack.Server.Web.Pages;

public class UploadModel : PageModel
{
    private readonly ILogger<UploadModel> _logger;
    private readonly IConfiguration _configuration;

    public UploadModel(ILogger<UploadModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [BindProperty]
    public string? ApiKey { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a package file")]
    public IFormFile? PackageFile { get; set; }

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }

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

        if (PackageFile.Length > 100 * 1024 * 1024) // 100MB limit
        {
            Message = "Package file size cannot exceed 100MB.";
            IsSuccess = false;
            return Page();
        }

        try
        {
            var baseUrl = _configuration.GetValue<string>("NuGetServer:BaseUrl") ?? "http://localhost:5003";
            
            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();
            
            // Add the package file
            var fileContent = new StreamContent(PackageFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "package", PackageFile.FileName);

            // Add API key if provided
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-NuGet-ApiKey", ApiKey);
            }

            var response = await httpClient.PutAsync($"{baseUrl}/v3/push", content);

            if (response.IsSuccessStatusCode)
            {
                Message = $"Package '{PackageFile.FileName}' uploaded successfully!";
                IsSuccess = true;
                _logger.LogInformation("Package uploaded successfully: {FileName}", PackageFile.FileName);
                
                // Clear form
                ApiKey = null;
                PackageFile = null;
                ModelState.Clear();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Message = $"Upload failed: {response.StatusCode} - {errorContent}";
                IsSuccess = false;
                _logger.LogWarning("Package upload failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
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
}
