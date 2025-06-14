#!/usr/bin/env pwsh
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "🔧 SWAGGER UI FIX VERIFICATION" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow
Write-Host ""

$ErrorActionPreference = "Continue"
$allPassed = $true

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedContent = $null
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
            
            if ($ExpectedContent -and $response.Content -notlike "*$ExpectedContent*") {
                Write-Host "⚠️  WARNING - Expected content not found: $ExpectedContent" -ForegroundColor Yellow
                $script:allPassed = $false
            }
            
            # Show content type and size
            $contentType = $response.Headers.'Content-Type'
            $contentLength = $response.Content.Length
            Write-Host "   Content-Type: $contentType" -ForegroundColor Gray
            Write-Host "   Content-Length: $contentLength bytes" -ForegroundColor Gray
        } else {
            Write-Host "❌ FAILED - Status: $($response.StatusCode)" -ForegroundColor Red
            $script:allPassed = $false
        }
    }
    catch {
        Write-Host "❌ ERROR - $($_.Exception.Message)" -ForegroundColor Red
        $script:allPassed = $false
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
    
    try {
        $json = Invoke-RestMethod -Uri $Url -TimeoutSec 10
        
        Write-Host "✅ SUCCESS - JSON parsed successfully" -ForegroundColor Green
        
        if ($ExpectedProperty) {
            $propertyValue = $json.$ExpectedProperty
            if ($propertyValue) {
                Write-Host "✅ Property '$ExpectedProperty': $propertyValue" -ForegroundColor Green
            } else {
                Write-Host "⚠️  WARNING - Property '$ExpectedProperty' not found" -ForegroundColor Yellow
                $script:allPassed = $false
            }
        }
        
        # Show JSON structure
        $keys = $json.PSObject.Properties.Name -join ', '
        Write-Host "   JSON Keys: $keys" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ ERROR - $($_.Exception.Message)" -ForegroundColor Red
        $script:allPassed = $false
    }
    
    Write-Host ""
}

# Test 1: Swagger Endpoints
Write-Host "=== SWAGGER UI TESTS ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Swagger JSON Schema" -Url "$ServerUrl/swagger/v1/swagger.json" -ExpectedProperty "openapi"
Test-Endpoint -Name "Swagger UI Page" -Url "$ServerUrl/swagger" -ExpectedContent "swagger-ui"

# Test 2: V3 API Endpoints
Write-Host "=== V3 API TESTS ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Service Index" -Url "$ServerUrl/v3/index.json" -ExpectedProperty "version"
Test-JsonEndpoint -Name "Search Service" -Url "$ServerUrl/v3/search?q=TestPackage" -ExpectedProperty "totalHits"
Test-JsonEndpoint -Name "Package Versions" -Url "$ServerUrl/v3-flatcontainer/testpackage/index.json" -ExpectedProperty "versions"
Test-JsonEndpoint -Name "Registration Index" -Url "$ServerUrl/v3/registrations/testpackage/index.json" -ExpectedProperty "count"

# Test 3: Web UI and Health
Write-Host "=== WEB UI & HEALTH TESTS ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Health Check" -Url "$ServerUrl/health" -ExpectedProperty "status"
Test-Endpoint -Name "Web UI" -Url "$ServerUrl/ui" -ExpectedContent "Nupack Server"

# Test 4: Advanced Search Features
Write-Host "=== ADVANCED SEARCH TESTS ===" -ForegroundColor Cyan
Test-JsonEndpoint -Name "Search with Prerelease" -Url "$ServerUrl/v3/search?prerelease=true" -ExpectedProperty "data"
Test-JsonEndpoint -Name "Search with Pagination" -Url "$ServerUrl/v3/search?skip=0&take=5" -ExpectedProperty "totalHits"
Test-JsonEndpoint -Name "Empty Search" -Url "$ServerUrl/v3/search" -ExpectedProperty "totalHits"

# Summary
Write-Host "=== FINAL RESULTS ===" -ForegroundColor Cyan

if ($allPassed) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host "✅ Swagger UI is working correctly" -ForegroundColor Green
    Write-Host "✅ V3 API endpoints are functional" -ForegroundColor Green
    Write-Host "✅ Web UI and health checks are operational" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔗 Access Points:" -ForegroundColor Cyan
    Write-Host "   Swagger UI: $ServerUrl/swagger" -ForegroundColor White
    Write-Host "   Service Index: $ServerUrl/v3/index.json" -ForegroundColor White
    Write-Host "   Web UI: $ServerUrl/ui" -ForegroundColor White
    Write-Host "   Health Check: $ServerUrl/health" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "❌ SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "Please check the server logs and configuration." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🚀 Swagger UI Fix Verification Complete!" -ForegroundColor Cyan
