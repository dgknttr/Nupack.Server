namespace Nupack.Server.Web.Models;

public class BrandingOptions
{
    public const string SectionName = "Branding";

    // Fixed branding values - not configurable
    public string ProductName { get; set; } = "Nupack Server";
    public string RepositoryTitle { get; set; } = "Nupack Server";
    public string HeaderBadgeText { get; set; } = "Package Repository";
    public string RepositoryDescription { get; set; } = "Open-source NuGet package repository powered by Nupack Server";
    public string NugetSourceName { get; set; } = "Nupack Server";
    public string FooterText { get; set; } = "Nupack Server";
    public string WelcomeMessage { get; set; } = "Discover and install .NET packages with ease.";
    public string ConfigurationGuideTitle { get; set; } = "Configure NuGet Client";
    public string DotNetCliGuide { get; set; } = "dotnet CLI";
    public string VisualStudioGuide { get; set; } = "Visual Studio";
    public string VisualStudioInstructions { get; set; } = "Tools → Options → NuGet Package Manager → Package Sources → Add new source with URL:";

    // Configurable values - can be customized per deployment
    public string CompanyName { get; set; } = "Your Organization";
}
