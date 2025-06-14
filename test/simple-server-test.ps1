#!/usr/bin/env pwsh

# Simple Server Test - Start server and run basic tests
param(
    [string]$ServerUrl = "http://localhost:5003"
)

Write-Host "🚀 Starting Simple Server Test" -ForegroundColor Cyan

# Kill any existing processes on port 5003
Write-Host "Checking for existing processes on port 5003..." -ForegroundColor Yellow
try {
    $processes = Get-NetTCPConnection -LocalPort 5003 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
    foreach ($pid in $processes) {
        Write-Host "Killing process $pid" -ForegroundColor Yellow
        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    }
} catch {
    Write-Host "No processes found on port 5003" -ForegroundColor Green
}

# Start server in background
Write-Host "Starting server..." -ForegroundColor Yellow
$serverPath = "../src/Nupack.Server.Api"

if (Test-Path $serverPath) {
    # Build first
    Write-Host "Building server..." -ForegroundColor Yellow
    $buildOutput = & dotnet build $serverPath --verbosity quiet 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
        
        # Start server
        $serverJob = Start-Job -ScriptBlock {
            param($path, $url)
            Set-Location $path
            & dotnet run --urls $url --no-build
        } -ArgumentList $serverPath, $ServerUrl
        
        Write-Host "Server job started with ID: $($serverJob.Id)" -ForegroundColor Cyan
        
        # Wait for server to start
        Write-Host "Waiting for server to start..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # Test server
        $maxAttempts = 5
        $attempt = 1
        $serverReady = $false
        
        while ($attempt -le $maxAttempts -and -not $serverReady) {
            try {
                Write-Host "Attempt $attempt/$maxAttempts to connect..." -ForegroundColor Yellow
                $response = Invoke-WebRequest -Uri $ServerUrl -UseBasicParsing -TimeoutSec 5
                $serverReady = $true
                Write-Host "✅ Server is responding!" -ForegroundColor Green
            } catch {
                Write-Host "❌ Attempt $attempt failed: $($_.Exception.Message)" -ForegroundColor Red
                Start-Sleep -Seconds 3
                $attempt++
            }
        }
        
        if ($serverReady) {
            # Run comprehensive test
            Write-Host "`n🧪 Running comprehensive tests..." -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File "comprehensive-enhanced-test.ps1" -ServerUrl $ServerUrl
        } else {
            Write-Host "❌ Server failed to start properly" -ForegroundColor Red
            
            # Check job output for errors
            $jobOutput = Receive-Job -Job $serverJob
            if ($jobOutput) {
                Write-Host "Server output:" -ForegroundColor Yellow
                Write-Host $jobOutput -ForegroundColor White
            }
        }
        
        # Cleanup
        Write-Host "`n🧹 Cleaning up..." -ForegroundColor Yellow
        Stop-Job -Job $serverJob -ErrorAction SilentlyContinue
        Remove-Job -Job $serverJob -ErrorAction SilentlyContinue
        
    } else {
        Write-Host "❌ Build failed:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
    }
} else {
    Write-Host "❌ Server project not found at: $serverPath" -ForegroundColor Red
}

Write-Host "`n🏁 Simple server test complete!" -ForegroundColor Cyan
