# Simple package upload test

$packagePath = "TestPackage.1.0.0.nupkg"
$uri = "http://localhost:5000/api/v1/packages"

Write-Host "Uploading package: $packagePath" -ForegroundColor Green

if (-not (Test-Path $packagePath)) {
    Write-Host "Package file not found: $packagePath" -ForegroundColor Red
    exit 1
}

try {
    # Read file as bytes
    $fileBytes = [System.IO.File]::ReadAllBytes($packagePath)
    $fileName = [System.IO.Path]::GetFileName($packagePath)

    # Create boundary
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"

    # Create multipart form data
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"package`"; filename=`"$fileName`"",
        "Content-Type: application/octet-stream",
        "",
        [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes),
        "--$boundary--"
    )

    $body = $bodyLines -join $LF
    $contentType = "multipart/form-data; boundary=$boundary"

    Write-Host "Sending upload request..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $body -ContentType $contentType

    Write-Host "Upload successful!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 3
}
catch {
    Write-Host "Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
        $errorContent = $_.Exception.Response.GetResponseStream()
        if ($errorContent) {
            $reader = New-Object System.IO.StreamReader($errorContent)
            $errorText = $reader.ReadToEnd()
            Write-Host "Error Details: $errorText" -ForegroundColor Yellow
        }
    }
}
