namespace Nupack.Server.Web.Models;

public class SearchResponse
{
    public int TotalHits { get; set; }
    public List<PackageSearchResult> Data { get; set; } = new();
}

public class PackageSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? Title { get; set; }
    public string? IconUrl { get; set; }
    public string? LicenseUrl { get; set; }
    public string? ProjectUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Authors { get; set; } = new();
    public long TotalDownloads { get; set; }
    public bool Verified { get; set; }
    public List<PackageVersion> Versions { get; set; } = new();

    public bool IsPrerelease => Version.Contains('-');
    public string DisplayTitle => Title ?? Id;
    public string DisplayDescription => Description ?? Summary ?? "No description available";
    public string LatestVersion => Versions.FirstOrDefault()?.Version ?? Version;
    public bool HasMultipleVersions => Versions.Count > 1;
    public int VersionCount => Versions.Count;
    public string AuthorsDisplay => Authors.Any() ? string.Join(", ", Authors) : "Unknown";
    public string TagsDisplay => Tags.Any() ? string.Join(", ", Tags.Take(3)) : "";
    public bool HasMoreTags => Tags.Count > 3;
    public int ExtraTagsCount => Math.Max(0, Tags.Count - 3);
}

public class PackageVersion
{
    public string Version { get; set; } = string.Empty;
    public long Downloads { get; set; }
    public string Id { get; set; } = string.Empty;
    
    public bool IsPrerelease => Version.Contains('-');
}

public class PackageVersionsIndex
{
    public List<string> Versions { get; set; } = new();
}

public class RegistrationIndex
{
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<RegistrationPage> Items { get; set; } = new();
}

public class RegistrationPage
{
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<RegistrationLeaf> Items { get; set; } = new();
    public string Lower { get; set; } = string.Empty;
    public string Upper { get; set; } = string.Empty;
}

public class RegistrationLeaf
{
    public string Id { get; set; } = string.Empty;
    public CatalogEntry CatalogEntry { get; set; } = new();
    public string PackageContent { get; set; } = string.Empty;
    public string Registration { get; set; } = string.Empty;
}

public class CatalogEntry
{
    public string Id { get; set; } = string.Empty;
    public string? Authors { get; set; }
    public List<DependencyGroup> DependencyGroups { get; set; } = new();
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string IdPackage { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? LicenseUrl { get; set; }
    public bool Listed { get; set; } = true;
    public string? MinClientVersion { get; set; }
    public string PackageContent { get; set; } = string.Empty;
    public string? ProjectUrl { get; set; }
    public DateTime Published { get; set; }
    public bool RequireLicenseAcceptance { get; set; }
    public string? Summary { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Title { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    
    public bool IsPrerelease => Version.Contains('-');
    public string DisplayTitle => Title ?? IdPackage;
    public string DisplayDescription => Description ?? Summary ?? "No description available";
}

public class DependencyGroup
{
    public string? Id { get; set; }
    public List<Dependency> Dependencies { get; set; } = new();
    public string? TargetFramework { get; set; }
}

public class Dependency
{
    public string Id { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string? Registration { get; set; }
}

public class ServiceIndex
{
    public string Version { get; set; } = string.Empty;
    public List<ServiceResource> Resources { get; set; } = new();
}

public class ServiceResource
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsHealthy => Status.Equals("healthy", StringComparison.OrdinalIgnoreCase);
}
