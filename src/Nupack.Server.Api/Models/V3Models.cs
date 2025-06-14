using System.Text.Json.Serialization;

namespace Nupack.Server.Api.Models.V3;

// NuGet V3 Service Index
public class ServiceIndex
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "3.0.0";

    [JsonPropertyName("resources")]
    public List<ServiceResource> Resources { get; set; } = new();
}

public class ServiceResource
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

// Search Service Models
public class SearchResponse
{
    [JsonPropertyName("totalHits")]
    public int TotalHits { get; set; }

    [JsonPropertyName("data")]
    public List<SearchResult> Data { get; set; } = new();
}

public class SearchResult
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "Package";

    [JsonPropertyName("registration")]
    public string Registration { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("licenseUrl")]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("authors")]
    public List<string> Authors { get; set; } = new();

    [JsonPropertyName("totalDownloads")]
    public long TotalDownloads { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("packageTypes")]
    public List<PackageType> PackageTypes { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<SearchVersion> Versions { get; set; } = new();
}

public class SearchVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;
}

public class PackageType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Dependency";
}

// Package Base Address (Flat Container) Models
public class PackageVersionsIndex
{
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
}

// Registration Models
public class RegistrationIndex
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public List<string> Type { get; set; } = new() { "catalog:CatalogRoot", "PackageRegistration", "catalog:Permalink" };

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<RegistrationPage> Items { get; set; } = new();
}

public class RegistrationPage
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "catalog:CatalogPage";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<RegistrationLeaf> Items { get; set; } = new();

    [JsonPropertyName("lower")]
    public string Lower { get; set; } = string.Empty;

    [JsonPropertyName("upper")]
    public string Upper { get; set; } = string.Empty;
}

public class RegistrationLeaf
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "Package";

    [JsonPropertyName("catalogEntry")]
    public CatalogEntry CatalogEntry { get; set; } = new();

    [JsonPropertyName("packageContent")]
    public string PackageContent { get; set; } = string.Empty;

    [JsonPropertyName("registration")]
    public string Registration { get; set; } = string.Empty;
}

public class CatalogEntry
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "PackageDetails";

    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("dependencyGroups")]
    public List<DependencyGroup> DependencyGroups { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("id")]
    public string Id_Package { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("licenseUrl")]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("listed")]
    public bool Listed { get; set; } = true;

    [JsonPropertyName("minClientVersion")]
    public string? MinClientVersion { get; set; }

    [JsonPropertyName("packageContent")]
    public string PackageContent { get; set; } = string.Empty;

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("published")]
    public DateTime Published { get; set; }

    [JsonPropertyName("requireLicenseAcceptance")]
    public bool RequireLicenseAcceptance { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

public class DependencyGroup
{
    [JsonPropertyName("@id")]
    public string? Id { get; set; }

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "PackageDependencyGroup";

    [JsonPropertyName("dependencies")]
    public List<Dependency> Dependencies { get; set; } = new();

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }
}

public class Dependency
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "PackageDependency";

    [JsonPropertyName("id")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }
}
