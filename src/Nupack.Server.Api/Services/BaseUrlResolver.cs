using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Nupack.Server.Api.Services;

/// <summary>
/// Centralized service for resolving base URLs in the NuGet V3 API.
/// Implements DRY principle by eliminating repeated base URL construction logic.
/// Follows SRP by having a single responsibility: base URL resolution.
/// Environment-aware: Configuration fallback only allowed in Development environment.
/// </summary>
public class BaseUrlResolver : IBaseUrlResolver
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BaseUrlResolver> _logger;

    public BaseUrlResolver(IConfiguration configuration, IWebHostEnvironment environment, ILogger<BaseUrlResolver> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the base URL using environment-aware logic:
    /// 1. HttpContext.Request.Scheme + Host (preferred - reflects actual request)
    /// 2. Configuration "NuGetServer:BaseUrl" (ONLY in Development environment)
    /// 3. Throws exception (fail-fast principle for non-Development environments)
    /// </summary>
    public string Resolve(HttpContext? context = null)
    {
        // Priority 1: Use HttpContext if available (most accurate)
        if (context?.Request != null)
        {
            try
            {
                var request = context.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";

                // Sanitize the base URL for logging to prevent log forging attacks
                _logger.LogDebug("Resolved base URL from HttpContext: {BaseUrl}", SanitizeForLogging(baseUrl));
                return baseUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve base URL from HttpContext");

                // In non-Development environments, fail immediately if HttpContext fails
                if (!_environment.IsDevelopment())
                {
                    var errorMessage = $"Failed to resolve base URL from HttpContext in {_environment.EnvironmentName} environment. " +
                                     "Configuration fallback is only allowed in Development environment. " +
                                     "Ensure the application is receiving valid HTTP requests with proper Host headers.";
                    _logger.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        // Priority 2: Fall back to configuration (ONLY in Development environment)
        if (_environment.IsDevelopment())
        {
            var configuredBaseUrl = _configuration.GetValue<string>("NuGetServer:BaseUrl")
                                  ?? _configuration.GetValue<string>("BaseUrl");

            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                // Sanitize the configured base URL for logging to prevent log forging attacks
                _logger.LogDebug("Resolved base URL from configuration in Development environment: {BaseUrl}", SanitizeForLogging(configuredBaseUrl));
                return configuredBaseUrl;
            }
        }

        // Priority 3: Fail fast with environment-specific error message
        var finalErrorMessage = _environment.IsDevelopment()
            ? "Unable to resolve base URL in Development environment. Either provide HttpContext or configure 'NuGetServer:BaseUrl' in appsettings.Development.json"
            : $"Unable to resolve base URL in {_environment.EnvironmentName} environment. HttpContext with valid Host header is required. Configuration fallback is only allowed in Development environment.";

        _logger.LogError(finalErrorMessage);
        throw new InvalidOperationException(finalErrorMessage);
    }

    /// <summary>
    /// Sanitizes a string for safe logging by removing characters that could be used for log forging attacks.
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>A sanitized string safe for logging</returns>
    private static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace(Environment.NewLine, "")
                   .Replace("\r", "")
                   .Replace("\n", "")
                   .Replace("\t", "");
    }
}
