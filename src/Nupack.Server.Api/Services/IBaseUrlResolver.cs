namespace Nupack.Server.Api.Services;

/// <summary>
/// Service responsible for resolving the base URL for the NuGet V3 API.
/// This centralizes base URL logic and ensures consistent URL generation
/// across all API endpoints and services.
/// Environment-aware: Configuration fallback only allowed in Development environment.
/// </summary>
public interface IBaseUrlResolver
{
    /// <summary>
    /// Resolves the base URL for the current request context with environment-aware fallback.
    /// </summary>
    /// <param name="context">The HTTP context to derive the base URL from. Required in non-Development environments.</param>
    /// <returns>The resolved base URL (e.g., "https://api.example.com" or "http://localhost:5003")</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when base URL cannot be resolved. In Development environment, falls back to configuration.
    /// In other environments (Stage, Test, Production), requires valid HttpContext with Host header.
    /// This ensures fail-fast behavior and prevents misconfiguration in production.
    /// </exception>
    string Resolve(HttpContext? context = null);
}
