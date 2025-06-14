# Test API endpoints

$baseUri = "http://localhost:5003"

Write-Host "Testing Nupack NuGet Server API..." -ForegroundColor Green

# Test 1: Get packages (should return empty list)
Write-Host "`n1. Testing GET /api/v1/packages" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUri/api/v1/packages" -Method Get
    Write-Host "✅ Success: Found $($response.data.totalCount) packages" -ForegroundColor Green
    if ($response.data.packages.Count -gt 0) {
        $response.data.packages | ForEach-Object {
            Write-Host "   - $($_.id) v$($_.version)" -ForegroundColor Cyan
        }
    }
}
catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Test Web UI
Write-Host "`n2. Testing GET /packages (Web UI)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUri/packages" -Method Get
    if ($response.StatusCode -eq 200 -and $response.Content.Contains("Nupack Server")) {
        Write-Host "✅ Success: Web UI is accessible" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed: Unexpected response" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Test Swagger
Write-Host "`n3. Testing GET /swagger (API Documentation)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUri/swagger" -Method Get
    if ($response.StatusCode -eq 200 -and $response.Content.Contains("Swagger")) {
        Write-Host "✅ Success: Swagger UI is accessible" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed: Unexpected response" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Test non-existent package
Write-Host "`n4. Testing GET /api/v1/packages/NonExistent/1.0.0" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUri/api/v1/packages/NonExistent/1.0.0" -Method Get
    Write-Host "❌ Unexpected success" -ForegroundColor Red
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✅ Success: Correctly returned 404 for non-existent package" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed: Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🎯 API Testing Complete!" -ForegroundColor Green
