#!/usr/bin/env pwsh

# Test Package Parsing
Write-Host "Testing Package Parsing..." -ForegroundColor Cyan

$packagesDir = "../src/Nupack.Server.Api/packages"
if (Test-Path $packagesDir) {
    $packages = Get-ChildItem -Path $packagesDir -Filter "*.nupkg"
    Write-Host "Found $($packages.Count) packages:" -ForegroundColor Green
    
    foreach ($package in $packages) {
        Write-Host "  - $($package.Name)" -ForegroundColor White
        
        # Try to extract metadata using NuGet tools
        try {
            # Rename to zip and extract
            $tempDir = [System.IO.Path]::GetTempPath() + [System.Guid]::NewGuid().ToString()
            New-Item -Path $tempDir -ItemType Directory | Out-Null
            
            $zipPath = Join-Path $tempDir "$($package.BaseName).zip"
            Copy-Item $package.FullName $zipPath
            
            # Extract
            Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force
            
            # Find .nuspec file
            $nuspecFile = Get-ChildItem -Path $tempDir -Filter "*.nuspec" -Recurse | Select-Object -First 1
            
            if ($nuspecFile) {
                Write-Host "    Found nuspec: $($nuspecFile.Name)" -ForegroundColor Yellow
                
                # Parse XML
                [xml]$nuspec = Get-Content $nuspecFile.FullName
                $metadata = $nuspec.package.metadata
                
                Write-Host "    ID: $($metadata.id)" -ForegroundColor Cyan
                Write-Host "    Version: $($metadata.version)" -ForegroundColor Cyan
                Write-Host "    Title: $($metadata.title)" -ForegroundColor Cyan
                Write-Host "    Description: $($metadata.description)" -ForegroundColor Cyan
                Write-Host "    Authors: $($metadata.authors)" -ForegroundColor Cyan
                Write-Host "    IsPrerelease: $($metadata.version -match '-')" -ForegroundColor Cyan
                
                if ($metadata.dependencies) {
                    Write-Host "    Has Dependencies: Yes" -ForegroundColor Cyan
                } else {
                    Write-Host "    Has Dependencies: No" -ForegroundColor Cyan
                }
            } else {
                Write-Host "    No nuspec file found" -ForegroundColor Red
            }
            
            # Cleanup
            Remove-Item $tempDir -Recurse -Force
            
        } catch {
            Write-Host "    Error parsing: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host ""
    }
} else {
    Write-Host "Packages directory not found: $packagesDir" -ForegroundColor Red
}

Write-Host "Package parsing test complete!" -ForegroundColor Green
