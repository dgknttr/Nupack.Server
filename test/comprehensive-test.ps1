# Comprehensive test suite for Nupack NuGet Server

param(
    [string]$ServerUrl = "http://localhost:5002"
)

Write-Host "Nupack NuGet Server - Comprehensive Test Suite" -ForegroundColor Green
Write-Host "Server URL: $ServerUrl" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

$testResults = @()

function Test-Endpoint {
    param($Name, $Url, $Method = "GET", $ExpectedStatus = 200)
    
    Write-Host "`nTesting: $Name" -ForegroundColor Yellow
    Write-Host "   URL: $Method $Url" -ForegroundColor Gray
    
    try {
        if ($Method -eq "GET") {
            $response = Invoke-WebRequest -Uri $Url -Method $Method -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -ErrorAction Stop
        }
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "   PASS: Status $($response.StatusCode)" -ForegroundColor Green
            return @{ Name = $Name; Status = "PASS"; Details = "Status $($response.StatusCode)" }
        } else {
            Write-Host "   FAIL: Expected $ExpectedStatus, got $($response.StatusCode)" -ForegroundColor Red
            return @{ Name = $Name; Status = "FAIL"; Details = "Wrong status code" }
        }
    }
    catch {
        Write-Host "   FAIL: $($_.Exception.Message)" -ForegroundColor Red
        return @{ Name = $Name; Status = "FAIL"; Details = $_.Exception.Message }
    }
}

function Test-ApiEndpoint {
    param($Name, $Url, $Method = "GET")
    
    Write-Host "`nTesting: $Name" -ForegroundColor Yellow
    Write-Host "   URL: $Method $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method $Method -ErrorAction Stop
        
        if ($response.success -eq $true) {
            Write-Host "   PASS: API returned success" -ForegroundColor Green
            return @{ Name = $Name; Status = "PASS"; Details = "API success" }
        } else {
            Write-Host "   FAIL: API returned failure" -ForegroundColor Red
            return @{ Name = $Name; Status = "FAIL"; Details = "API failure" }
        }
    }
    catch {
        Write-Host "   FAIL: $($_.Exception.Message)" -ForegroundColor Red
        return @{ Name = $Name; Status = "FAIL"; Details = $_.Exception.Message }
    }
}

# Test 1: Web UI
$testResults += Test-Endpoint "Web UI Home Page" "$ServerUrl/"
$testResults += Test-Endpoint "Web UI Packages Page" "$ServerUrl/packages"

# Test 2: API Documentation
$testResults += Test-Endpoint "Swagger Documentation" "$ServerUrl/swagger"

# Test 3: API Endpoints
$testResults += Test-ApiEndpoint "List Packages (Empty)" "$ServerUrl/api/v1/packages"
$testResults += Test-ApiEndpoint "Search Packages" "$ServerUrl/api/v1/packages?q=test"
$testResults += Test-ApiEndpoint "Pagination Test" "$ServerUrl/api/v1/packages?skip=0&take=5"

# Test 4: Error Handling
Write-Host "`nTesting: Non-existent Package (404 Expected)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ServerUrl/api/v1/packages/NonExistent/1.0.0" -Method GET -ErrorAction Stop
    Write-Host "   FAIL: Should have returned 404" -ForegroundColor Red
    $testResults += @{ Name = "404 Error Handling"; Status = "FAIL"; Details = "No 404 returned" }
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "   PASS: Correctly returned 404" -ForegroundColor Green
        $testResults += @{ Name = "404 Error Handling"; Status = "PASS"; Details = "404 returned correctly" }
    } else {
        Write-Host "   FAIL: Wrong error: $($_.Exception.Message)" -ForegroundColor Red
        $testResults += @{ Name = "404 Error Handling"; Status = "FAIL"; Details = $_.Exception.Message }
    }
}

# Test 5: Static Files
$testResults += Test-Endpoint "Static Files Middleware" "$ServerUrl/packages" 200

# Summary
Write-Host "`n" + "=" * 60 -ForegroundColor Gray
Write-Host "📊 TEST RESULTS SUMMARY" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Gray

$passCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$totalCount = $testResults.Count

foreach ($result in $testResults) {
    $icon = if ($result.Status -eq "PASS") { "✅" } else { "❌" }
    $color = if ($result.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "$icon $($result.Name): $($result.Status)" -ForegroundColor $color
    if ($result.Status -eq "FAIL") {
        Write-Host "   Details: $($result.Details)" -ForegroundColor Gray
    }
}

Write-Host "`n📈 OVERALL RESULTS:" -ForegroundColor Cyan
Write-Host "   Total Tests: $totalCount" -ForegroundColor White
Write-Host "   Passed: $passCount" -ForegroundColor Green
Write-Host "   Failed: $failCount" -ForegroundColor Red
Write-Host "   Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 1))%" -ForegroundColor Cyan

if ($failCount -eq 0) {
    Write-Host "`n🎉 ALL TESTS PASSED! Server is working correctly." -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some tests failed. Please check the server configuration." -ForegroundColor Yellow
}

Write-Host "`n🔗 Quick Links:" -ForegroundColor Cyan
Write-Host "   Web UI: $ServerUrl/packages" -ForegroundColor White
Write-Host "   API Docs: $ServerUrl/swagger" -ForegroundColor White
Write-Host "   API Base: $ServerUrl/api/v1/packages" -ForegroundColor White
