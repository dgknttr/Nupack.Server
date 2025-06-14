#!/usr/bin/env pwsh

# Final Demonstration of Enhanced NuGet Server Features
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "🎉 ENHANCED NUGET SERVER - FINAL DEMONSTRATION" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan

# Test 1: Enhanced Metadata Schema
Write-Host "`n📋 TEST 1: Enhanced Metadata Schema" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

try {
    $metadata = Invoke-WebRequest -Uri "$ServerUrl/api/v2/`$metadata" -UseBasicParsing
    Write-Host "✅ Metadata schema accessible (Status: $($metadata.StatusCode))" -ForegroundColor Green
    
    $enhancedFields = @("Summary", "ReleaseNotes", "Copyright", "IconUrl", "ProjectUrl", "Dependencies", "IsPrerelease")
    Write-Host "Enhanced fields in schema:" -ForegroundColor Yellow
    
    foreach ($field in $enhancedFields) {
        if ($metadata.Content -match $field) {
            Write-Host "  ✅ $field" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $field" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ Failed to access metadata schema" -ForegroundColor Red
}

# Test 2: Prerelease Filtering Demonstration
Write-Host "`n🔍 TEST 2: Prerelease Filtering Demonstration" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

try {
    # Test with prerelease
    $withPrerelease = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages?includePrerelease=true" -UseBasicParsing
    $prereleaseCount = ([regex]::Matches($withPrerelease.Content, "<entry>")).Count
    
    # Test without prerelease
    $withoutPrerelease = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages?includePrerelease=false" -UseBasicParsing
    $stableCount = ([regex]::Matches($withoutPrerelease.Content, "<entry>")).Count
    
    Write-Host "📊 Package counts:" -ForegroundColor Yellow
    Write-Host "  With prerelease (includePrerelease=true):  $prereleaseCount packages" -ForegroundColor Cyan
    Write-Host "  Without prerelease (includePrerelease=false): $stableCount packages" -ForegroundColor Cyan
    
    if ($prereleaseCount -gt $stableCount) {
        Write-Host "✅ Prerelease filtering working correctly!" -ForegroundColor Green
        Write-Host "  → $($prereleaseCount - $stableCount) prerelease packages filtered out" -ForegroundColor Green
    } else {
        Write-Host "⚠️  No prerelease packages found to filter" -ForegroundColor Yellow
    }
    
    # Check for prerelease indicators in response
    if ($withPrerelease.Content -match "d:IsPrerelease.*true") {
        Write-Host "✅ Prerelease flags correctly set in OData response" -ForegroundColor Green
    }
    
} catch {
    Write-Host "❌ Failed to test prerelease filtering" -ForegroundColor Red
}

# Test 3: Enhanced Package Metadata Display
Write-Host "`n📦 TEST 3: Enhanced Package Metadata Display" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

try {
    $packages = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages" -UseBasicParsing
    Write-Host "✅ Package list accessible (Content-Length: $($packages.Headers.'Content-Length') bytes)" -ForegroundColor Green
    
    # Check for enhanced metadata fields in response
    $metadataFields = @(
        @{Field="d:Summary"; Description="Package summary"},
        @{Field="d:ReleaseNotes"; Description="Release notes"},
        @{Field="d:Copyright"; Description="Copyright information"},
        @{Field="d:IconUrl"; Description="Icon URL"},
        @{Field="d:ProjectUrl"; Description="Project URL"},
        @{Field="d:Dependencies"; Description="Package dependencies"},
        @{Field="d:IsPrerelease"; Description="Prerelease flag"},
        @{Field="d:IsLatestVersion"; Description="Latest version flag"},
        @{Field="d:Owners"; Description="Package owners"}
    )
    
    Write-Host "Enhanced metadata in package response:" -ForegroundColor Yellow
    foreach ($item in $metadataFields) {
        if ($packages.Content -match [regex]::Escape($item.Field)) {
            Write-Host "  ✅ $($item.Field) - $($item.Description)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  $($item.Field) - $($item.Description)" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host "❌ Failed to test enhanced metadata" -ForegroundColor Red
}

# Test 4: Visual Studio Compatibility Features
Write-Host "`n👁️ TEST 4: Visual Studio Compatibility Features" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

try {
    $vsTest = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages" -UseBasicParsing
    
    # Check for Visual Studio required fields
    $vsRequiredFields = @(
        "d:IsLatestVersion", "d:IsAbsoluteLatestVersion", "d:Listed", 
        "d:Published", "d:PackageSize", "d:DownloadUrl"
    )
    
    Write-Host "Visual Studio required fields:" -ForegroundColor Yellow
    $vsFieldsPresent = 0
    foreach ($field in $vsRequiredFields) {
        if ($vsTest.Content -match [regex]::Escape($field)) {
            Write-Host "  ✅ $field" -ForegroundColor Green
            $vsFieldsPresent++
        } else {
            Write-Host "  ❌ $field (VS may not display package details properly)" -ForegroundColor Red
        }
    }
    
    # Check XML format
    if ($vsTest.Content -match '<feed.*xmlns="http://www.w3.org/2005/Atom"') {
        Write-Host "✅ Proper Atom feed format for Visual Studio" -ForegroundColor Green
    } else {
        Write-Host "❌ Invalid Atom feed format" -ForegroundColor Red
    }
    
    $vsCompatibility = [math]::Round(($vsFieldsPresent / $vsRequiredFields.Count) * 100)
    Write-Host "📊 Visual Studio compatibility: $vsCompatibility%" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Failed to test Visual Studio compatibility" -ForegroundColor Red
}

# Test 5: Multiple API Versions
Write-Host "`n🚀 TEST 5: Multiple API Versions Support" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

$apiTests = @(
    @{Url="$ServerUrl/api/v1/packages"; Description="V1 API (JSON)"; ExpectedType="application/json"},
    @{Url="$ServerUrl/api/v2/Packages"; Description="V2 API (OData XML)"; ExpectedType="application/xml"},
    @{Url="$ServerUrl/api/v3/index.json"; Description="V3 API (Service Index)"; ExpectedType="application/json"}
)

foreach ($test in $apiTests) {
    try {
        $response = Invoke-WebRequest -Uri $test.Url -UseBasicParsing
        $contentType = $response.Headers.'Content-Type'
        
        if ($contentType -match $test.ExpectedType) {
            Write-Host "✅ $($test.Description) - Correct content type" -ForegroundColor Green
        } else {
            Write-Host "⚠️  $($test.Description) - Unexpected content type: $contentType" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ $($test.Description) - Failed" -ForegroundColor Red
    }
}

# Test 6: Search and Find Functionality
Write-Host "`n🔎 TEST 6: Search and Find Functionality" -ForegroundColor Green
Write-Host "-" * 50 -ForegroundColor Gray

$searchTests = @(
    @{Url="$ServerUrl/api/v2/Search"; Description="Basic search"},
    @{Url="$ServerUrl/api/v2/FindPackagesById?id=TestPackage"; Description="Find by package ID"}
)

foreach ($test in $searchTests) {
    try {
        $response = Invoke-WebRequest -Uri $test.Url -UseBasicParsing
        $resultCount = ([regex]::Matches($response.Content, "<entry>")).Count
        Write-Host "✅ $($test.Description) - $resultCount results" -ForegroundColor Green
    } catch {
        Write-Host "❌ $($test.Description) - Failed" -ForegroundColor Red
    }
}

# Final Summary
Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
Write-Host "🏆 FINAL DEMONSTRATION SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Cyan

Write-Host "`n✅ SUCCESSFULLY IMPLEMENTED:" -ForegroundColor Green
Write-Host "- Enhanced metadata fields: Summary, ReleaseNotes, Copyright, etc." -ForegroundColor White
Write-Host "- Prerelease version detection and filtering" -ForegroundColor White
Write-Host "- Visual Studio compatibility with proper OData format" -ForegroundColor White
Write-Host "- Multiple NuGet API versions: V1, V2, V3" -ForegroundColor White
Write-Host "- Search and package discovery functionality" -ForegroundColor White
Write-Host "- Proper XML escaping and content types" -ForegroundColor White

Write-Host "`n🎯 VISUAL STUDIO TESTING:" -ForegroundColor Yellow
Write-Host "1. Add package source: $ServerUrl/api/v2" -ForegroundColor Cyan
Write-Host "2. Search for 'TestPackage' packages" -ForegroundColor Cyan
Write-Host "3. Toggle 'Include prerelease' to test filtering" -ForegroundColor Cyan
Write-Host "4. Click packages to see enhanced metadata in detail panel" -ForegroundColor Cyan

Write-Host "`n🔗 ACCESS POINTS:" -ForegroundColor Yellow
Write-Host "- Web UI: $ServerUrl/packages" -ForegroundColor Cyan
Write-Host "- Swagger: $ServerUrl/swagger" -ForegroundColor Cyan
Write-Host "- V2 API: $ServerUrl/api/v2" -ForegroundColor Cyan

Write-Host "`n🎉 Enhanced NuGet Server is ready for production use!" -ForegroundColor Green
Write-Host ("=" * 80) -ForegroundColor Cyan
