# 📚 Usage Examples

## .NET CLI Commands

### Adding the Package Source
```bash
# Add Nupack NuGet server as a package source
dotnet nuget add source http://localhost:5003/v3/index.json --name "Nupack Server"

# List all configured sources
dotnet nuget list source
```

### Publishing Packages
```bash
# Create a sample package first (if you don't have one)
dotnet new classlib -n SampleLibrary
cd SampleLibrary
dotnet pack

# Push the package to Nupack NuGet server
dotnet nuget push bin/Debug/SampleLibrary.1.0.0.nupkg --source "Nupack Server"

# Push with specific API endpoint
dotnet nuget push bin/Debug/SampleLibrary.1.0.0.nupkg --source http://localhost:5003/api/v2/package
```

### Installing Packages
```bash
# Install from Nupack server specifically
dotnet add package SampleLibrary --source "Nupack Server"

# Install with version specification
dotnet add package SampleLibrary --version 1.0.0 --source "Nupack Server"
```

## cURL Examples

### Upload Package
```bash
# Upload a .nupkg file
curl -X POST "http://localhost:8080/api/v1/packages" \
  -H "Content-Type: multipart/form-data" \
  -F "package=@SampleLibrary.1.0.0.nupkg"

# Expected response:
# {
#   "success": true,
#   "data": {
#     "id": "SampleLibrary",
#     "version": "1.0.0",
#     "title": "SampleLibrary",
#     "description": "Package Description",
#     "authors": "Author Name",
#     "tags": null,
#     "created": "2024-06-04T15:09:00Z",
#     "size": 4096,
#     "fileName": "SampleLibrary.1.0.0.nupkg"
#   },
#   "message": "Package uploaded successfully"
# }
```

### Search Packages
```bash
# Get all packages
curl "http://localhost:8080/api/v1/packages"

# Search with query
curl "http://localhost:8080/api/v1/packages?q=Sample"

# Pagination
curl "http://localhost:8080/api/v1/packages?skip=0&take=10"

# Combined search with pagination
curl "http://localhost:8080/api/v1/packages?q=Library&skip=0&take=5"
```

### Get Package Metadata
```bash
# Get specific package metadata
curl "http://localhost:8080/api/v1/packages/SampleLibrary/1.0.0"

# Expected response:
# {
#   "success": true,
#   "data": {
#     "id": "SampleLibrary",
#     "version": "1.0.0",
#     "title": "SampleLibrary",
#     "description": "Package Description",
#     "authors": "Author Name",
#     "tags": null,
#     "created": "2024-06-04T15:09:00Z",
#     "size": 4096,
#     "fileName": "SampleLibrary.1.0.0.nupkg"
#   }
# }
```

### Download Package
```bash
# Download package file
curl -O "http://localhost:8080/api/v1/packages/SampleLibrary/1.0.0/download"

# Download with custom filename
curl -o "MyCustomName.nupkg" "http://localhost:8080/api/v1/packages/SampleLibrary/1.0.0/download"

# Download and verify
curl -I "http://localhost:8080/api/v1/packages/SampleLibrary/1.0.0/download"
```

### Delete Package
```bash
# Delete a specific package version
curl -X DELETE "http://localhost:8080/api/v1/packages/SampleLibrary/1.0.0"

# Expected response:
# {
#   "success": true,
#   "data": true,
#   "message": "Package deleted successfully"
# }
```

## PowerShell Examples

### Upload Package
```powershell
# Upload using Invoke-RestMethod
$packagePath = "SampleLibrary.1.0.0.nupkg"
$uri = "http://localhost:8080/api/v1/packages"

$form = @{
    package = Get-Item $packagePath
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
Write-Output $response
```

### Search and Download
```powershell
# Search packages
$searchUri = "http://localhost:8080/api/v1/packages?q=Sample"
$packages = Invoke-RestMethod -Uri $searchUri -Method Get

# Display results
$packages.data.packages | Format-Table Id, Version, Description

# Download first package
if ($packages.data.packages.Count -gt 0) {
    $pkg = $packages.data.packages[0]
    $downloadUri = "http://localhost:8080/api/v1/packages/$($pkg.id)/$($pkg.version)/download"
    Invoke-WebRequest -Uri $downloadUri -OutFile "$($pkg.id).$($pkg.version).nupkg"
}
```

## Visual Studio Integration

### Package Manager Console
```powershell
# In Visual Studio Package Manager Console

# Install package from Nupack server
Install-Package SampleLibrary -Source "Nupack Server"

# Update package
Update-Package SampleLibrary -Source "Nupack Server"

# Uninstall package
Uninstall-Package SampleLibrary
```

### Package Manager UI
1. Go to **Tools** → **NuGet Package Manager** → **Manage NuGet Packages for Solution**
2. Click on **Settings** (gear icon)
3. Add new source:
   - **Name**: Nupack Server
   - **Source**: http://localhost:5003/v3/index.json
4. Select "Nupack Server" from the package source dropdown
5. Browse and install packages

## Testing the Server

### Health Check
```bash
# Simple health check
curl -f "http://localhost:8080/api/v1/packages" && echo "Server is healthy"

# Check if server is responding
curl -I "http://localhost:8080/packages"
```

### Load Testing with Apache Bench
```bash
# Test package listing endpoint
ab -n 100 -c 10 "http://localhost:8080/api/v1/packages"

# Test search endpoint
ab -n 50 -c 5 "http://localhost:8080/api/v1/packages?q=test"
```

## Automation Scripts

### Batch Upload Script (Bash)
```bash
#!/bin/bash
# upload-packages.sh

NUGET_SERVER="http://localhost:8080/api/v1/packages"
PACKAGES_DIR="./packages"

for package in "$PACKAGES_DIR"/*.nupkg; do
    if [ -f "$package" ]; then
        echo "Uploading $package..."
        curl -X POST "$NUGET_SERVER" \
             -H "Content-Type: multipart/form-data" \
             -F "package=@$package"
        echo ""
    fi
done
```

### Batch Upload Script (PowerShell)
```powershell
# upload-packages.ps1

$nugetServer = "http://localhost:8080/api/v1/packages"
$packagesDir = "./packages"

Get-ChildItem -Path $packagesDir -Filter "*.nupkg" | ForEach-Object {
    Write-Host "Uploading $($_.Name)..."
    
    $form = @{ package = $_ }
    try {
        $response = Invoke-RestMethod -Uri $nugetServer -Method Post -Form $form
        if ($response.success) {
            Write-Host "✅ Successfully uploaded $($_.Name)" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to upload $($_.Name): $($response.message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error uploading $($_.Name): $($_.Exception.Message)" -ForegroundColor Red
    }
}
```
