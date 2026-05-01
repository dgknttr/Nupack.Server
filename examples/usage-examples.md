# Usage Examples

This document shows the current supported V3-style usage for Nupack Server.

## Add the Feed

```bash
dotnet nuget add source "http://localhost:5003/v3/index.json" --name "Nupack Server"
```

List configured sources:

```bash
dotnet nuget list source
```

## Push a Package

```bash
dotnet nuget push path/to/YourPackage.1.0.0.nupkg --source "Nupack Server"
```

`Development` allows blank write auth by default for local reference use. Outside `Development`, configure `PackageSecurity:WriteApiKey` and pass it with `--api-key`, or explicitly set `PackageSecurity:AllowAnonymousWrites=true`.

```bash
dotnet nuget push path/to/YourPackage.1.0.0.nupkg --source "Nupack Server" --api-key "your-write-key"
```

Or push directly to the URL:

```bash
dotnet nuget push path/to/YourPackage.1.0.0.nupkg --source "http://localhost:5003/v3/index.json"
```

## Restore a Package From the Feed

```bash
dotnet add package YourPackage --source "Nupack Server"
```

If you already committed a `nuget.config` file with the feed, a normal restore is enough:

```bash
dotnet restore
```

## Browse the Feed in a Browser

- Web UI: `http://localhost:5004`
- Service index: `http://localhost:5003/v3/index.json`
- Swagger: `http://localhost:5003/swagger`

## Storage Configuration Examples

### Filesystem

```json
{
  "PackageStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "data/packages"
    }
  }
}
```

### S3-compatible / MinIO

```json
{
  "PackageStorage": {
    "Provider": "S3",
    "S3": {
      "BucketName": "nupack-packages",
      "Region": "us-east-1",
      "ServiceUrl": "http://localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "Prefix": "packages/",
      "ForcePathStyle": true
    }
  }
}
```

## V3 API Examples

### Service index

```bash
curl http://localhost:5003/v3/index.json
```

### Search

```bash
curl "http://localhost:5003/v3/search?q=TestPackage&take=10"
```

### Package versions

```bash
curl http://localhost:5003/v3-flatcontainer/testpackage/index.json
```

### Download package

```bash
curl -O http://localhost:5003/v3-flatcontainer/testpackage/1.0.0/testpackage.1.0.0.nupkg
```

### Download nuspec

```bash
curl http://localhost:5003/v3-flatcontainer/testpackage/1.0.0/testpackage.nuspec
```

### Registration index

```bash
curl http://localhost:5003/v3/registrations/testpackage/index.json
```

### Delete package

```bash
curl -X DELETE http://localhost:5003/v3/delete/TestPackage/1.0.0
```

`Development` allows blank write auth by default for local reference use. Outside `Development`, configure `PackageSecurity:WriteApiKey` and send it with `X-NuGet-ApiKey`, or explicitly set `PackageSecurity:AllowAnonymousWrites=true`.

```bash
curl -X DELETE http://localhost:5003/v3/delete/TestPackage/1.0.0 -H "X-NuGet-ApiKey: your-write-key"
```

## Notes

- built-in auth is intentionally minimal: blank write auth is open by default only in `Development`; outside `Development`, `push` and `delete` require `PackageSecurity:WriteApiKey` or explicit `PackageSecurity:AllowAnonymousWrites=true`
- storage providers in the repo today are `FileSystem` and `S3`
- download statistics and unlisting are not supported yet
- the separate Web app is the official UI; the API `/ui` and `/frontend` routes are legacy demos
