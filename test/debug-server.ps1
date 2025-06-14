#!/usr/bin/env pwsh

# Debug Server Startup Issues
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "Debugging NuGet Server Startup" -ForegroundColor Cyan

# Check if server is running
Write-Host "`n1. Testing server connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $ServerUrl -UseBasicParsing -TimeoutSec 5
    Write-Host "✅ Server is responding on $ServerUrl" -ForegroundColor Green
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Server not responding on $ServerUrl" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host "`nAttempting to start server..." -ForegroundColor Yellow
    
    # Try to start the server
    $serverPath = "src/Nupack.Server.Api"
    if (Test-Path $serverPath) {
        Write-Host "Found server project at: $serverPath" -ForegroundColor Green
        
        # Build first
        Write-Host "Building project..." -ForegroundColor Yellow
        $buildResult = & dotnet build $serverPath 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build successful" -ForegroundColor Green
            
            # Start server in background
            Write-Host "Starting server..." -ForegroundColor Yellow
            $serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $serverPath, "--urls", $ServerUrl -PassThru -WindowStyle Hidden
            
            # Wait a bit for startup
            Start-Sleep -Seconds 10
            
            # Test again
            try {
                $response = Invoke-WebRequest -Uri $ServerUrl -UseBasicParsing -TimeoutSec 5
                Write-Host "✅ Server started successfully!" -ForegroundColor Green
                Write-Host "   PID: $($serverProcess.Id)" -ForegroundColor Cyan
            } catch {
                Write-Host "❌ Server still not responding after startup attempt" -ForegroundColor Red
                if (!$serverProcess.HasExited) {
                    $serverProcess.Kill()
                }
            }
        } else {
            Write-Host "❌ Build failed:" -ForegroundColor Red
            Write-Host $buildResult -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Server project not found at: $serverPath" -ForegroundColor Red
    }
}

# Test basic endpoints
Write-Host "`n2. Testing basic endpoints..." -ForegroundColor Yellow

$endpoints = @(
    "/",
    "/api/v1/packages",
    "/api/v2",
    "/api/v2/`$metadata",
    "/api/v3/index.json",
    "/swagger"
)

foreach ($endpoint in $endpoints) {
    try {
        $url = "$ServerUrl$endpoint"
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        Write-Host "✅ $endpoint - Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ $endpoint - Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n3. Checking for existing packages..." -ForegroundColor Yellow
$packagesDir = "src/Nupack.Server.Api/packages"
if (Test-Path $packagesDir) {
    $packages = Get-ChildItem -Path $packagesDir -Filter "*.nupkg"
    Write-Host "Found $($packages.Count) packages:" -ForegroundColor Cyan
    foreach ($package in $packages) {
        Write-Host "   - $($package.Name)" -ForegroundColor White
    }
} else {
    Write-Host "Packages directory not found: $packagesDir" -ForegroundColor Yellow
    Write-Host "   Creating packages directory..." -ForegroundColor Yellow
    New-Item -Path $packagesDir -ItemType Directory -Force | Out-Null
}

Write-Host "`nDebug complete!" -ForegroundColor Cyan
