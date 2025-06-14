# PowerShell script to create a test NuGet package

Write-Host "Creating test NuGet package..." -ForegroundColor Green

# Create temporary directory for test package
$testDir = "TestPackage"
if (Test-Path $testDir) {
    Remove-Item $testDir -Recurse -Force
}

# Create new class library project
dotnet new classlib -n $testDir -o $testDir

# Navigate to project directory
Set-Location $testDir

# Update project file with package metadata
$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>TestPackage</PackageId>
    <PackageVersion>2.0.0-alpha.1</PackageVersion>
    <Title>Test Package - Advanced Features</Title>
    <Description>A comprehensive test package for Nupack NuGet Server demonstration with rich metadata support. This package includes advanced features for testing Visual Studio integration, prerelease handling, and metadata display.</Description>
    <Summary>Advanced test package for NuGet server testing with comprehensive metadata</Summary>
    <Authors>Development Team;John Doe;Jane Smith</Authors>
    <Owners>Your Organization</Owners>
    <Company>Your Company</Company>
    <PackageTags>test;demo;alpha;nuget;server;metadata;visual-studio</PackageTags>
    <PackageReleaseNotes>
      Version 2.0.0-alpha.1:
      - Added comprehensive metadata support
      - Enhanced Visual Studio integration
      - Improved prerelease version handling
      - Added dependency management features
    </PackageReleaseNotes>
    <Copyright>Copyright © 2025 Your Organization. All rights reserved.</Copyright>
    <PackageProjectUrl>https://github.com/yourorg/nupack-server</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageIconUrl>https://yourcompany.com/icon.png</PackageIconUrl>
    <RepositoryUrl>https://github.com/yourorg/nupack-server</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

</Project>
"@

$csprojContent | Out-File -FilePath "TestPackage.csproj" -Encoding UTF8

# Create a simple class
$classContent = @"
namespace TestPackage;

/// <summary>
/// A simple test class for demonstration purposes
/// </summary>
public class TestHelper
{
    /// <summary>
    /// Returns a greeting message
    /// </summary>
    /// <param name="name">The name to greet</param>
    /// <returns>A greeting message</returns>
    public static string GetGreeting(string name)
    {
        return `$"Hello {name} from Test Package!";
    }

    /// <summary>
    /// Returns the current version of the package
    /// </summary>
    /// <returns>Package version</returns>
    public static string GetVersion()
    {
        return "2.0.0-alpha.1";
    }
}
"@

$classContent | Out-File -FilePath "TestHelper.cs" -Encoding UTF8

# Build and pack the project
Write-Host "Building and packing the test package..." -ForegroundColor Yellow
dotnet build
dotnet pack -c Release

# Move the package to the parent directory
$packageFile = Get-ChildItem -Path "bin/Release" -Filter "*.nupkg" | Select-Object -First 1
if ($packageFile) {
    Copy-Item $packageFile.FullName -Destination "../$($packageFile.Name)"
    Write-Host "Test package created: $($packageFile.Name)" -ForegroundColor Green
    Write-Host "Package location: $(Resolve-Path "../$($packageFile.Name)")" -ForegroundColor Cyan
} else {
    Write-Host "Failed to create package" -ForegroundColor Red
}

# Return to parent directory
Set-Location ..

# Clean up
Remove-Item $testDir -Recurse -Force

Write-Host "Test package creation completed!" -ForegroundColor Green
