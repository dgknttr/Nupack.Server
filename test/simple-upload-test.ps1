# Simple upload test using .NET WebClient

$packagePath = "TestPackage.1.0.0.nupkg"
$uri = "http://localhost:5003/api/v2/package"

Write-Host "Testing package upload with HttpClient..." -ForegroundColor Green

$fileStream = $null
$client = $null

try {
    # Use HttpClient for multipart upload
    Add-Type -AssemblyName System.Net.Http

    $client = New-Object System.Net.Http.HttpClient
    $content = New-Object System.Net.Http.MultipartFormDataContent

    $fileStream = [System.IO.File]::OpenRead($packagePath)
    $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")

    $content.Add($fileContent, "package", [System.IO.Path]::GetFileName($packagePath))

    Write-Host "Sending request..." -ForegroundColor Yellow
    $response = $client.PostAsync($uri, $content).Result
    $responseContent = $response.Content.ReadAsStringAsync().Result

    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    Write-Host "Response: $responseContent" -ForegroundColor Cyan
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Inner Exception: $($_.Exception.InnerException.Message)" -ForegroundColor Red
}
finally {
    if ($fileStream) {
        $fileStream.Close()
        $fileStream.Dispose()
    }
    if ($client) {
        $client.Dispose()
    }
}
