# Simple test for Nupack NuGet Server

param(
    [string]$ServerUrl = "http://localhost:5002"
)

Write-Host "Nupack NuGet Server - Simple Test" -ForegroundColor Green
Write-Host "Server URL: $ServerUrl" -ForegroundColor Cyan
Write-Host "=" * 50

$tests = @()

# Test 1: API Health Check
Write-Host "`nTest 1: API Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ServerUrl/api/v1/packages" -Method GET
    if ($response.success) {
        Write-Host "PASS: API is responding" -ForegroundColor Green
        $tests += "PASS"
    } else {
        Write-Host "FAIL: API returned error" -ForegroundColor Red
        $tests += "FAIL"
    }
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $tests += "FAIL"
}

# Test 2: Web UI
Write-Host "`nTest 2: Web UI Access" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ServerUrl/packages" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "PASS: Web UI is accessible" -ForegroundColor Green
        $tests += "PASS"
    } else {
        Write-Host "FAIL: Wrong status code" -ForegroundColor Red
        $tests += "FAIL"
    }
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $tests += "FAIL"
}

# Test 3: Swagger Documentation
Write-Host "`nTest 3: Swagger Documentation" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ServerUrl/swagger" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "PASS: Swagger is accessible" -ForegroundColor Green
        $tests += "PASS"
    } else {
        Write-Host "FAIL: Wrong status code" -ForegroundColor Red
        $tests += "FAIL"
    }
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $tests += "FAIL"
}

# Test 4: Package Search
Write-Host "`nTest 4: Package Search" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ServerUrl/api/v1/packages?q=test" -Method GET
    if ($response.success) {
        Write-Host "PASS: Search endpoint working" -ForegroundColor Green
        $tests += "PASS"
    } else {
        Write-Host "FAIL: Search returned error" -ForegroundColor Red
        $tests += "FAIL"
    }
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $tests += "FAIL"
}

# Summary
$passCount = ($tests | Where-Object { $_ -eq "PASS" }).Count
$totalCount = $tests.Count

Write-Host "`n" + "=" * 50
Write-Host "RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "Passed: $passCount / $totalCount" -ForegroundColor White
Write-Host "Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 1))%" -ForegroundColor White

if ($passCount -eq $totalCount) {
    Write-Host "`nALL TESTS PASSED! Server is working correctly." -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed. Check server status." -ForegroundColor Yellow
}

Write-Host "`nQuick Links:" -ForegroundColor Cyan
Write-Host "Web UI: $ServerUrl/packages"
Write-Host "API Docs: $ServerUrl/swagger"
Write-Host "API: $ServerUrl/api/v1/packages"
