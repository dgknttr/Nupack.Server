# Package Storage Configuration

Nupack Server uses an explicit storage provider model. `FileSystem` is the default. `S3` is the first bucket-backed provider and works with AWS S3, MinIO, and compatible endpoints.

## Preferred Configuration Shape

```json
{
  "PackageStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "data/packages"
    },
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

## Provider Selection

### FileSystem

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

When `BasePath` is empty, packages are stored in a local `packages` folder under the app root.

### S3-Compatible

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

`ServiceUrl` is optional for AWS S3 and recommended for MinIO or other S3-compatible endpoints.

## Legacy Compatibility

The legacy shorthand still works and maps to the filesystem provider:

```json
{
  "PackageStorage": {
    "BasePath": "data/packages"
  }
}
```

This is preserved so existing installs do not break, but new docs and examples should prefer `Provider + FileSystem`.

## Docker Examples

### Default filesystem run

```yaml
services:
  nupack-server:
    volumes:
      - nupack-data:/app/data
    environment:
      - PackageStorage__Provider=FileSystem
      - PackageStorage__FileSystem__BasePath=/app/data/packages
```

The repository compose file declares `nupack-data` as a Docker-managed named volume. This keeps `/app/data` writable by the image's non-root `appuser` on first run. If a development override replaces it with a host bind mount, create that directory with permissions that allow the container user to write; production deployments should keep a managed volume or an equivalently provisioned persistent volume.

### MinIO-backed run

```bash
PACKAGE_STORAGE_PROVIDER=S3 docker compose --profile s3 up --build
```

The compose file already includes:
- `PackageStorage__S3__BucketName`
- `PackageStorage__S3__Region`
- `PackageStorage__S3__ServiceUrl`
- `PackageStorage__S3__AccessKey`
- `PackageStorage__S3__SecretKey`
- `PackageStorage__S3__ForcePathStyle`
- `PackageStorage__S3__Prefix`

## Operational Notes

- filesystem storage keeps the current on-disk package layout
- S3 storage stores objects as `{prefix}{id-lower}/{version-lower}/{id-lower}.{version-lower}.nupkg`
- both providers rebuild metadata by scanning stored `.nupkg` files/objects at startup
- prefer environment variables or secret stores for S3 credentials
- for MinIO and local dev, keep `ForcePathStyle=true`

### Storage readiness timeout

`GET /health/ready` and its compatibility alias `GET /health` probe the selected storage provider. The probe timeout defaults to five seconds and can be configured alongside storage settings:

```json
{
  "PackageHealth": {
    "ReadinessTimeoutSeconds": 5
  }
}
```

The valid range is `1` through `300` seconds. Missing, malformed, non-positive, or greater-than-300 values fall back to the five-second default. Container and other environment-based deployments can use:

```text
PackageHealth__ReadinessTimeoutSeconds=5
```

`GET /health/live` does not invoke storage and is not affected by this timeout.
