# PowerShell script to upload test package

$packagePath = "TestPackage.1.0.0.nupkg"
$uri = "http://localhost:5003/api/v2/package"

Write-Host "Uploading package: $packagePath" -ForegroundColor Green

try {
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    # Read file content
    $fileContent = [System.IO.File]::ReadAllBytes($packagePath)
    $fileName = [System.IO.Path]::GetFileName($packagePath)
    
    # Create form data
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"package`"; filename=`"$fileName`"",
        "Content-Type: application/octet-stream",
        "",
        [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileContent),
        "--$boundary--"
    )
    
    $body = $bodyLines -join $LF
    
    # Upload
    $response = Invoke-RestMethod -Uri $uri -Method Post -ContentType "multipart/form-data; boundary=$boundary" -Body $body
    
    Write-Host "Upload successful!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
}
catch {
    Write-Host "Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Full error: $($_.Exception)" -ForegroundColor Yellow
}
