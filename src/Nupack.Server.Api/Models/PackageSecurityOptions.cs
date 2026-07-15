namespace Nupack.Server.Api.Models;

public class PackageSecurityOptions
{
    public const string SectionName = "PackageSecurity";

    public string? PublishApiKey { get; set; }

    public string? DeleteApiKey { get; set; }

    // Compatibility fallback for 0.x configurations. Prefer operation-specific keys.
    public string? WriteApiKey { get; set; }

    public bool AllowAnonymousWrites { get; set; }
}
