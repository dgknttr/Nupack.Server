namespace Nupack.Server.Api.Models;

public class PackageSecurityOptions
{
    public const string SectionName = "PackageSecurity";

    public string? WriteApiKey { get; set; }

    public bool AllowAnonymousWrites { get; set; }
}
