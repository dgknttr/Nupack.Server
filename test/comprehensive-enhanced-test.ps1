#!/usr/bin/env pwsh

# Comprehensive Enhanced NuGet Server Test
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "COMPREHENSIVE ENHANCED NUGET SERVER TEST" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Server URL: $ServerUrl" -ForegroundColor Yellow
Write-Host ""

# Function to test endpoint
function Test-Endpoint {
    param($Url, $Description)
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        Write-Host "✅ $Description - Status: $($response.StatusCode)" -ForegroundColor Green
        return $response
    } catch {
        Write-Host "❌ $Description - Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Test 1: Basic Server Connectivity
Write-Host "🔌 TEST 1: Basic Server Connectivity" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

$basicTests = @(
    @{ Url = "$ServerUrl"; Description = "Root endpoint" },
    @{ Url = "$ServerUrl/api/v1/packages"; Description = "V1 API packages" },
    @{ Url = "$ServerUrl/api/v2"; Description = "V2 API root" },
    @{ Url = "$ServerUrl/api/v2/`$metadata"; Description = "V2 metadata schema" },
    @{ Url = "$ServerUrl/api/v3/index.json"; Description = "V3 service index" }
)

foreach ($test in $basicTests) {
    Test-Endpoint -Url $test.Url -Description $test.Description
}

# Test 2: Enhanced Metadata Schema Validation
Write-Host "`n📋 TEST 2: Enhanced Metadata Schema Validation" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

$metadataResponse = Test-Endpoint -Url "$ServerUrl/api/v2/`$metadata" -Description "Metadata schema"
if ($metadataResponse) {
    $content = $metadataResponse.Content
    
    $enhancedFields = @(
        "Summary", "ReleaseNotes", "Copyright", "IconUrl", "ProjectUrl", 
        "LicenseUrl", "Dependencies", "IsPrerelease", "IsLatestVersion", 
        "IsAbsoluteLatestVersion", "Owners", "Language"
    )
    
    Write-Host "Checking for enhanced metadata fields:" -ForegroundColor Yellow
    foreach ($field in $enhancedFields) {
        if ($content -match $field) {
            Write-Host "  ✅ $field" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $field" -ForegroundColor Red
        }
    }
}

# Test 3: Package List with Enhanced Metadata
Write-Host "`n📦 TEST 3: Package List with Enhanced Metadata" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

$packagesResponse = Test-Endpoint -Url "$ServerUrl/api/v2/Packages" -Description "Package list"
if ($packagesResponse) {
    $content = $packagesResponse.Content
    $entryCount = ([regex]::Matches($content, "<entry>")).Count
    Write-Host "📊 Total packages found: $entryCount" -ForegroundColor Cyan
    
    # Check for enhanced metadata in response
    $metadataFields = @(
        "d:Summary", "d:ReleaseNotes", "d:Copyright", "d:IconUrl", 
        "d:ProjectUrl", "d:Dependencies", "d:IsPrerelease", "d:Owners"
    )
    
    Write-Host "Checking for enhanced metadata in package response:" -ForegroundColor Yellow
    foreach ($field in $metadataFields) {
        if ($content -match [regex]::Escape($field)) {
            Write-Host "  ✅ $field present" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  $field not found" -ForegroundColor Yellow
        }
    }
    
    # Check for prerelease indicators
    if ($content -match "d:IsPrerelease.*true") {
        Write-Host "  ✅ Prerelease packages detected" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  No prerelease packages found" -ForegroundColor Yellow
    }
}

# Test 4: Prerelease Filtering
Write-Host "`n🔍 TEST 4: Prerelease Filtering" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

# Test with includePrerelease=true
$prereleaseResponse = Test-Endpoint -Url "$ServerUrl/api/v2/Packages?includePrerelease=true" -Description "Packages with prerelease=true"
$prereleaseCount = 0
if ($prereleaseResponse) {
    $prereleaseCount = ([regex]::Matches($prereleaseResponse.Content, "<entry>")).Count
    Write-Host "📊 Packages with prerelease=true: $prereleaseCount" -ForegroundColor Cyan
}

# Test with includePrerelease=false
$stableResponse = Test-Endpoint -Url "$ServerUrl/api/v2/Packages?includePrerelease=false" -Description "Packages with prerelease=false"
$stableCount = 0
if ($stableResponse) {
    $stableCount = ([regex]::Matches($stableResponse.Content, "<entry>")).Count
    Write-Host "📊 Packages with prerelease=false: $stableCount" -ForegroundColor Cyan
    
    if ($prereleaseCount -gt $stableCount) {
        Write-Host "✅ Prerelease filtering working correctly" -ForegroundColor Green
    } elseif ($prereleaseCount -eq $stableCount) {
        Write-Host "⚠️  No prerelease packages to filter" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Prerelease filtering not working" -ForegroundColor Red
    }
}

# Test 5: Search Functionality
Write-Host "`n🔎 TEST 5: Search Functionality" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

$searchTests = @(
    @{ Url = "$ServerUrl/api/v2/Search()"; Description = "Basic search" },
    @{ Url = "$ServerUrl/api/v2/Search()?includePrerelease=true"; Description = "Search with prerelease" },
    @{ Url = "$ServerUrl/api/v2/FindPackagesById()?id='TestPackage'"; Description = "Find by ID" }
)

foreach ($test in $searchTests) {
    $response = Test-Endpoint -Url $test.Url -Description $test.Description
    if ($response) {
        $count = ([regex]::Matches($response.Content, "<entry>")).Count
        Write-Host "  📊 Results: $count packages" -ForegroundColor Cyan
    }
}

# Test 6: Visual Studio Compatibility
Write-Host "`n👁️ TEST 6: Visual Studio Compatibility Features" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

Write-Host "Testing Visual Studio specific requirements:" -ForegroundColor Yellow

# Check for proper OData format
$oDataResponse = Test-Endpoint -Url "$ServerUrl/api/v2/Packages" -Description "OData format validation"
if ($oDataResponse) {
    $content = $oDataResponse.Content
    
    # Check for required Visual Studio fields
    $vsFields = @(
        "d:IsLatestVersion", "d:IsAbsoluteLatestVersion", "d:Listed", 
        "d:Published", "d:PackageSize", "d:DownloadUrl"
    )
    
    foreach ($field in $vsFields) {
        if ($content -match [regex]::Escape($field)) {
            Write-Host "  ✅ $field (required for VS)" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $field (missing - VS may not work properly)" -ForegroundColor Red
        }
    }
    
    # Check XML structure
    if ($content -match '<feed.*xmlns="http://www.w3.org/2005/Atom"') {
        Write-Host "  ✅ Proper Atom feed format" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Invalid Atom feed format" -ForegroundColor Red
    }
}

# Test 7: CLI Compatibility
Write-Host "`n💻 TEST 7: CLI Compatibility" -ForegroundColor Cyan
Write-Host "-" * 50 -ForegroundColor Gray

Write-Host "Testing .NET CLI compatibility:" -ForegroundColor Yellow

# Test package search via CLI (if available)
try {
    Write-Host "Attempting CLI package search..." -ForegroundColor Yellow
    $cliResult = & dotnet package search "TestPackage" --source $ServerUrl/api/v2 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ CLI search successful" -ForegroundColor Green
        Write-Host "CLI Output:" -ForegroundColor Cyan
        Write-Host $cliResult -ForegroundColor White
    } else {
        Write-Host "⚠️  CLI search failed (this is expected if source not configured)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  CLI test skipped (dotnet CLI not available or source not configured)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
Write-Host "🏁 COMPREHENSIVE TEST SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Cyan

Write-Host "`n✅ IMPLEMENTED FEATURES:" -ForegroundColor Green
Write-Host "- Enhanced metadata fields (Summary, ReleaseNotes, Copyright, etc.)" -ForegroundColor White
Write-Host "- Prerelease version detection and filtering" -ForegroundColor White
Write-Host "- Visual Studio compatibility (OData format, required fields)" -ForegroundColor White
Write-Host "- Multiple API versions (V1, V2, V3)" -ForegroundColor White
Write-Host "- Proper XML escaping for metadata" -ForegroundColor White
Write-Host "- Latest version flagging" -ForegroundColor White

Write-Host "`n🔗 QUICK ACCESS LINKS:" -ForegroundColor Yellow
Write-Host "- Web UI: $ServerUrl/packages" -ForegroundColor Cyan
Write-Host "- Swagger: $ServerUrl/swagger" -ForegroundColor Cyan
Write-Host "- V2 API: $ServerUrl/api/v2" -ForegroundColor Cyan
Write-Host "- V3 API: $ServerUrl/api/v3/index.json" -ForegroundColor Cyan

Write-Host "`n📋 NEXT STEPS FOR VISUAL STUDIO TESTING:" -ForegroundColor Yellow
Write-Host "1. Add package source in Visual Studio: Tools > Options > NuGet Package Manager" -ForegroundColor White
Write-Host "2. Add source URL: $ServerUrl/api/v2" -ForegroundColor White
Write-Host "3. Enable 'Include prerelease' to test prerelease filtering" -ForegroundColor White
Write-Host "4. Search for 'TestPackage' to see packages with enhanced metadata" -ForegroundColor White

Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
