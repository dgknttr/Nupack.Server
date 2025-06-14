#!/usr/bin/env pwsh
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "🚀 Testing Nupack NuGet Server V3 API" -ForegroundColor Cyan
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow
Write-Host ""

$ErrorActionPreference = "Continue"
$testResults = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedContent = $null
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
            
            if ($ExpectedContent -and $response.Content -notlike "*$ExpectedContent*") {
                Write-Host "⚠️  WARNING - Expected content not found: $ExpectedContent" -ForegroundColor Yellow
                $script:testResults += @{ Name = $Name; Status = "Warning"; Message = "Content check failed" }
            } else {
                $script:testResults += @{ Name = $Name; Status = "Success"; Message = "OK" }
            }
            
            # Show content preview for JSON responses
            if ($response.Headers.'Content-Type' -like "*json*") {
                $preview = $response.Content.Substring(0, [Math]::Min(200, $response.Content.Length))
                Write-Host "Preview: $preview..." -ForegroundColor Gray
            }
        } else {
            Write-Host "❌ FAILED - Status: $($response.StatusCode)" -ForegroundColor Red
            $script:testResults += @{ Name = $Name; Status = "Failed"; Message = "HTTP $($response.StatusCode)" }
        }
    }
    catch {
        Write-Host "❌ ERROR - $($_.Exception.Message)" -ForegroundColor Red
        $script:testResults += @{ Name = $Name; Status = "Error"; Message = $_.Exception.Message }
    }
    
    Write-Host ""
}

function Test-JsonEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedProperty = $null
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        $json = Invoke-RestMethod -Uri $Url -TimeoutSec 10
        
        Write-Host "✅ SUCCESS - JSON parsed successfully" -ForegroundColor Green
        
        if ($ExpectedProperty) {
            $propertyValue = $json.$ExpectedProperty
            if ($propertyValue) {
                Write-Host "✅ Property '$ExpectedProperty': $propertyValue" -ForegroundColor Green
                $script:testResults += @{ Name = $Name; Status = "Success"; Message = "OK" }
            } else {
                Write-Host "⚠️  WARNING - Property '$ExpectedProperty' not found" -ForegroundColor Yellow
                $script:testResults += @{ Name = $Name; Status = "Warning"; Message = "Property missing" }
            }
        } else {
            $script:testResults += @{ Name = $Name; Status = "Success"; Message = "OK" }
        }
        
        # Show JSON structure
        Write-Host "JSON Keys: $($json.PSObject.Properties.Name -join ', ')" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ ERROR - $($_.Exception.Message)" -ForegroundColor Red
        $script:testResults += @{ Name = $Name; Status = "Error"; Message = $_.Exception.Message }
    }
    
    Write-Host ""
}

# Test 1: Service Index
Write-Host "=== 1. SERVICE INDEX ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Service Index" -Url "$ServerUrl/v3/index.json" -ExpectedProperty "version"

# Test 2: Search Service
Write-Host "=== 2. SEARCH SERVICE ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Search All Packages" -Url "$ServerUrl/v3/search" -ExpectedProperty "totalHits"
Test-JsonEndpoint -Name "Search Test Packages" -Url "$ServerUrl/v3/search?q=TestPackage" -ExpectedProperty "data"
Test-JsonEndpoint -Name "Search with Prerelease" -Url "$ServerUrl/v3/search?prerelease=true" -ExpectedProperty "totalHits"
Test-JsonEndpoint -Name "Search with Pagination" -Url "$ServerUrl/v3/search?skip=0&take=10" -ExpectedProperty "data"

# Test 3: Package Base Address (Flat Container)
Write-Host "=== 3. PACKAGE BASE ADDRESS (FLAT CONTAINER) ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Package Versions Index" -Url "$ServerUrl/v3-flatcontainer/testpackage/index.json" -ExpectedProperty "versions"
Test-Endpoint -Name "Package Download" -Url "$ServerUrl/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg"
Test-Endpoint -Name "Package Manifest" -Url "$ServerUrl/v3-flatcontainer/testpackage/1.0.0/testpackage.nuspec"

# Test 4: Registration
Write-Host "=== 4. REGISTRATION ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Registration Index" -Url "$ServerUrl/v3/registrations/testpackage/index.json" -ExpectedProperty "count"
Test-JsonEndpoint -Name "Registration Leaf" -Url "$ServerUrl/v3/registrations/testpackage/1.0.0.json" -ExpectedProperty "catalogEntry"

# Test 5: Web UI and Health
Write-Host "=== 5. WEB UI & HEALTH ===" -ForegroundColor Cyan
Test-Endpoint -Name "Root Redirect" -Url "$ServerUrl/"
Test-JsonEndpoint -Name "Health Check" -Url "$ServerUrl/health" -ExpectedProperty "status"
Test-Endpoint -Name "Web UI" -Url "$ServerUrl/ui" -ExpectedContent "Nupack Server"
Test-Endpoint -Name "Swagger API Docs" -Url "$ServerUrl/swagger"

# Test 6: Error Handling
Write-Host "=== 6. ERROR HANDLING ===" -ForegroundColor Cyan
Write-Host "Testing: Non-existent Package" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ServerUrl/v3-flatcontainer/nonexistent.package/index.json" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 404) {
        Write-Host "✅ SUCCESS - Correctly returned 404 for non-existent package" -ForegroundColor Green
        $testResults += @{ Name = "404 Error Handling"; Status = "Success"; Message = "OK" }
    } else {
        Write-Host "⚠️  WARNING - Expected 404 but got $($response.StatusCode)" -ForegroundColor Yellow
        $testResults += @{ Name = "404 Error Handling"; Status = "Warning"; Message = "Unexpected status code" }
    }
}
catch {
    if ($_.Exception.Message -like "*404*") {
        Write-Host "✅ SUCCESS - Correctly returned 404 for non-existent package" -ForegroundColor Green
        $testResults += @{ Name = "404 Error Handling"; Status = "Success"; Message = "OK" }
    } else {
        Write-Host "❌ ERROR - $($_.Exception.Message)" -ForegroundColor Red
        $testResults += @{ Name = "404 Error Handling"; Status = "Error"; Message = $_.Exception.Message }
    }
}
Write-Host ""

# Summary
Write-Host "=== TEST SUMMARY ===" -ForegroundColor Cyan
$successCount = ($testResults | Where-Object { $_.Status -eq "Success" }).Count
$warningCount = ($testResults | Where-Object { $_.Status -eq "Warning" }).Count
$errorCount = ($testResults | Where-Object { $_.Status -eq "Error" }).Count
$totalCount = $testResults.Count

Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "✅ Successful: $successCount" -ForegroundColor Green
Write-Host "⚠️  Warnings: $warningCount" -ForegroundColor Yellow
Write-Host "❌ Errors: $errorCount" -ForegroundColor Red

if ($errorCount -eq 0 -and $warningCount -eq 0) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED! V3 API is fully functional!" -ForegroundColor Green
} elseif ($errorCount -eq 0) {
    Write-Host ""
    Write-Host "✅ Tests completed with warnings. V3 API is mostly functional." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ Some tests failed. Please check the V3 API implementation." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== DETAILED RESULTS ===" -ForegroundColor Cyan
$testResults | ForEach-Object {
    $color = switch ($_.Status) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "White" }
    }
    Write-Host "$($_.Name): $($_.Status) - $($_.Message)" -ForegroundColor $color
}

Write-Host ""
Write-Host "🔗 Quick Links:" -ForegroundColor Cyan
Write-Host "Service Index: $ServerUrl/v3/index.json" -ForegroundColor Gray
Write-Host "Search API: $ServerUrl/v3/search?q=TestPackage" -ForegroundColor Gray
Write-Host "Web UI: $ServerUrl/ui" -ForegroundColor Gray
Write-Host "Swagger: $ServerUrl/swagger" -ForegroundColor Gray
