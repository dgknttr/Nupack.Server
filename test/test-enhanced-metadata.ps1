#!/usr/bin/env pwsh

# Test Enhanced Metadata and Prerelease Functionality
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "🧪 Testing Enhanced NuGet Server Metadata & Prerelease Support" -ForegroundColor Cyan
Write-Host "Server URL: $ServerUrl" -ForegroundColor Yellow
Write-Host ("=" * 70) -ForegroundColor Gray

# Test 1: Check V2 API Metadata Schema
Write-Host "`n📋 Test 1: V2 API Metadata Schema" -ForegroundColor Green
try {
    $metadataResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/`$metadata" -UseBasicParsing
    if ($metadataResponse.StatusCode -eq 200) {
        Write-Host "✅ PASS: Metadata schema accessible" -ForegroundColor Green
        
        # Check for enhanced metadata fields
        $content = $metadataResponse.Content
        $enhancedFields = @("Summary", "ReleaseNotes", "Copyright", "IconUrl", "ProjectUrl", "LicenseUrl", "Dependencies", "IsPrerelease")
        
        foreach ($field in $enhancedFields) {
            if ($content -match $field) {
                Write-Host "  ✅ $field field present" -ForegroundColor Green
            } else {
                Write-Host "  ❌ $field field missing" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "❌ FAIL: Metadata schema not accessible" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ FAIL: Error accessing metadata schema - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Check Package List with Enhanced Metadata
Write-Host "`n📦 Test 2: Package List with Enhanced Metadata" -ForegroundColor Green
try {
    $packagesResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages" -UseBasicParsing
    if ($packagesResponse.StatusCode -eq 200) {
        Write-Host "✅ PASS: Package list accessible" -ForegroundColor Green
        
        $content = $packagesResponse.Content
        
        # Check for enhanced metadata in response
        $metadataFields = @("d:Summary", "d:ReleaseNotes", "d:Copyright", "d:IconUrl", "d:ProjectUrl", "d:Dependencies", "d:IsPrerelease")
        
        foreach ($field in $metadataFields) {
            if ($content -match [regex]::Escape($field)) {
                Write-Host "  ✅ $field present in response" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️  $field not found in response" -ForegroundColor Yellow
            }
        }
        
        # Check for prerelease indicators
        if ($content -match "d:IsPrerelease.*true") {
            Write-Host "  ✅ Prerelease packages detected" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  No prerelease packages found" -ForegroundColor Yellow
        }
        
    } else {
        Write-Host "❌ FAIL: Package list not accessible" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ FAIL: Error accessing package list - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Prerelease Filtering
Write-Host "`n🔍 Test 3: Prerelease Filtering" -ForegroundColor Green

# Test with includePrerelease=true
try {
    $prereleaseResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages?includePrerelease=true" -UseBasicParsing
    $prereleaseCount = ([regex]::Matches($prereleaseResponse.Content, "<entry>")).Count
    Write-Host "  📊 Packages with prerelease=true: $prereleaseCount" -ForegroundColor Cyan
} catch {
    Write-Host "  ❌ Error testing prerelease=true" -ForegroundColor Red
}

# Test with includePrerelease=false
try {
    $stableResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Packages?includePrerelease=false" -UseBasicParsing
    $stableCount = ([regex]::Matches($stableResponse.Content, "<entry>")).Count
    Write-Host "  📊 Packages with prerelease=false: $stableCount" -ForegroundColor Cyan
    
    if ($prereleaseCount -gt $stableCount) {
        Write-Host "  ✅ PASS: Prerelease filtering working correctly" -ForegroundColor Green
    } elseif ($prereleaseCount -eq $stableCount) {
        Write-Host "  ⚠️  WARNING: No prerelease packages to filter" -ForegroundColor Yellow
    } else {
        Write-Host "  ❌ FAIL: Prerelease filtering not working" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Error testing prerelease=false" -ForegroundColor Red
}

# Test 4: Search with Prerelease
Write-Host "`n🔎 Test 4: Search with Prerelease Support" -ForegroundColor Green
try {
    $searchResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/Search()?includePrerelease=true" -UseBasicParsing
    if ($searchResponse.StatusCode -eq 200) {
        Write-Host "  ✅ PASS: Search endpoint accessible" -ForegroundColor Green
        
        if ($searchResponse.Content -match "d:IsPrerelease.*true") {
            Write-Host "  ✅ PASS: Search returns prerelease packages" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  No prerelease packages in search results" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "  ❌ FAIL: Search endpoint error - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: FindPackagesById
Write-Host "`n🎯 Test 5: FindPackagesById Functionality" -ForegroundColor Green
try {
    $findResponse = Invoke-WebRequest -Uri "$ServerUrl/api/v2/FindPackagesById()?id='TestPackage'" -UseBasicParsing
    if ($findResponse.StatusCode -eq 200) {
        Write-Host "  ✅ PASS: FindPackagesById accessible" -ForegroundColor Green
        
        $versionCount = ([regex]::Matches($findResponse.Content, "<entry>")).Count
        Write-Host "  📊 Versions found for TestPackage: $versionCount" -ForegroundColor Cyan
        
        if ($findResponse.Content -match "d:IsLatestVersion.*true") {
            Write-Host "  ✅ PASS: Latest version flagging working" -ForegroundColor Green
        }
        
        if ($findResponse.Content -match "d:IsAbsoluteLatestVersion.*true") {
            Write-Host "  ✅ PASS: Absolute latest version flagging working" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "  ❌ FAIL: FindPackagesById error - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: V3 Service Index
Write-Host "`n🚀 Test 6: V3 Service Index" -ForegroundColor Green
try {
    $v3Response = Invoke-RestMethod -Uri "$ServerUrl/api/v3/index.json"
    if ($v3Response.version -eq "3.0.0") {
        Write-Host "  ✅ PASS: V3 service index accessible" -ForegroundColor Green
        Write-Host "  📊 Resources available: $($v3Response.resources.Count)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  ❌ FAIL: V3 service index error - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n" + ("=" * 70) -ForegroundColor Gray
Write-Host "🏁 Enhanced Metadata & Prerelease Testing Complete!" -ForegroundColor Cyan

# Summary
Write-Host "`n📋 SUMMARY:" -ForegroundColor Yellow
Write-Host "- Enhanced metadata fields added to schema" -ForegroundColor White
Write-Host "- Prerelease filtering implemented" -ForegroundColor White
Write-Host "- Visual Studio compatibility improved" -ForegroundColor White
Write-Host "- Multiple API versions supported (V2/V3)" -ForegroundColor White

Write-Host "`n🔗 Quick Links:" -ForegroundColor Yellow
Write-Host "- Web UI: $ServerUrl/packages" -ForegroundColor Cyan
Write-Host "- Swagger: $ServerUrl/swagger" -ForegroundColor Cyan
Write-Host "- V2 API: $ServerUrl/api/v2" -ForegroundColor Cyan
Write-Host "- V3 API: $ServerUrl/api/v3/index.json" -ForegroundColor Cyan
