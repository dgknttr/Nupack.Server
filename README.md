# Nupack Server

![CI](https://github.com/dgknttr/Nupack.Server/actions/workflows/ci.yml/badge.svg)
![Security](https://github.com/dgknttr/Nupack.Server/actions/workflows/security.yml/badge.svg)
![CodeQL](https://github.com/dgknttr/Nupack.Server/actions/workflows/codeql.yml/badge.svg)

Nupack Server is a lightweight, self-hosted **NuGet V3 server and feed** built with ASP.NET Core 9. Its primary use case is publishing a company's own internal `.nupkg` packages so developers and CI jobs can restore them from infrastructure the company controls.

**30-second pitch:** run a small internal feed with anonymous search/download and authenticated publishing, or fork and adapt the implementation to your environment.

It is designed to be:
- easy to fork
- easy to understand
- easy to customize
- honest about what is supported today

This repository is **not** trying to compete with full package platforms. The goal is a solid starter kit for teams, side projects, labs, and contributors who want a hackable NuGet feed they can run, study, and evolve. Nupack implements the server side of NuGet V3; it does not replace the NuGet client, the `.nupkg` format, or standard commands such as `dotnet restore` and `dotnet nuget push`.

## Quick Paths

- Want to try it now: jump to [Quickstart](#quickstart)
- Want to run it with containers: jump to [Docker-First Run](#docker-first-run)
- Want object storage: jump to [Package Storage](#package-storage)
- Want to contribute: start with [CONTRIBUTING.md](CONTRIBUTING.md)
- Want to understand the product direction: read the [needs analysis](docs/needs-analysis.md) and [roadmap](docs/roadmap.md)

## Why This Exists

Nupack Server exists first for .NET teams that need to publish and restore their own company packages without adopting a larger package platform. It also serves contributors who want a small, readable NuGet V3 server they can fork and reshape.

The default internal-feed model treats the company network, VPN, or reverse proxy as the read boundary: package search, metadata, and downloads are anonymous, while publish and delete are authenticated. The `0.1` direction separates publish and delete credentials because those operations carry different risks. Authentication for reads can be added at a reverse proxy or through customization, but it is optional and is not a `0.1` requirement.

## Who This Is For

Use this repo if you want to:
- run a small self-hosted NuGet feed with a separate web UI
- learn how the NuGet V3 protocol hangs together in ASP.NET Core
- fork a starter implementation and add your own auth, storage, or UI
- embed the ideas into your own solution later

This repo is a poor fit if you need:
- multi-tenant package hosting
- full built-in authentication and authorization
- unlisting or download analytics
- enterprise workflow features or operational guarantees

## Current Shape

The solution contains two app hosts and three storage projects:
- `src/Nupack.Server.Api`: the NuGet V3 API host
- `src/Nupack.Server.Web`: the official Razor Pages web UI
- `src/Nupack.Server.Storage`: shared storage contracts and metadata helpers
- `src/Nupack.Server.Storage.FileSystem`: default filesystem provider
- `src/Nupack.Server.Storage.S3`: S3-compatible provider for AWS S3, MinIO, and similar endpoints

The API also contains `/ui` and `/frontend` demo routes, but those are **legacy demo surfaces** and not part of the official supported UI story.

Package metadata is still built conservatively by scanning stored `.nupkg` files or objects at startup and caching the results in memory.

## What Works Today

| Capability | Status | Notes |
| --- | --- | --- |
| Service index | Supported | `/v3/index.json` |
| Search | Supported | Basic search, pagination, prerelease filter |
| Flat container versions | Supported | `/v3-flatcontainer/{id}/index.json` |
| Package download | Supported | `.nupkg` download endpoint |
| Registration index/page/leaf | Supported | Simplified registration model |
| Package push | Supported | `PUT /v3/push` |
| Package delete | Supported | `DELETE /v3/delete/{id}/{version}` |
| Health endpoint | Supported | `/health` |
| Web browse/search/upload | Supported | Separate Web app is the official UI |
| Nuspec endpoint | Supported | Returns extracted `.nuspec` XML |
| Storage providers | Supported | `FileSystem` and `S3` are built in |
| SemVer and prerelease handling | Partial | Core flows work; coverage is still growing |
| Authentication | Partial | Blank write auth is open by default only in `Development`; outside `Development`, `push` and `delete` require `PackageSecurity:WriteApiKey` or explicit `PackageSecurity:AllowAnonymousWrites=true` |
| Unlist | Not supported | |
| Download stats | Not supported | UI treats stats as unavailable |

## Non-Goals for the Current Release Line

The current `0.x` line is intentionally conservative.

Not in scope right now:
- full built-in auth, identity, or policy management
- enterprise positioning or compliance claims
- embeddable NuGet package distribution
- persistent external metadata indexes or database catalogs
- advanced admin workflows

## Quickstart

### Prerequisites
- .NET 9 SDK
- Docker (optional)

### 1. Clone and restore

```bash
git clone https://github.com/dgknttr/Nupack.Server.git
cd Nupack.Server
dotnet restore
```

The repo ships with a root `NuGet.Config` that clears machine-specific feeds and restores from `nuget.org`, so local setup does not depend on your global NuGet configuration.

### 2. Start the API and Web UI

```bash
# Terminal 1
dotnet run --project src/Nupack.Server.Api --urls "http://localhost:5003"

# Terminal 2
dotnet run --project src/Nupack.Server.Web --urls "http://localhost:5004"
```

### 3. Configure a client

```bash
dotnet nuget add source "http://localhost:5003/v3/index.json" --name "Nupack Server"
```

### 4. Push a package

```bash
dotnet nuget push path/to/YourPackage.1.0.0.nupkg --source "Nupack Server"
```

If your deployment configures a shared write key, include it on push:

```bash
dotnet nuget push path/to/YourPackage.1.0.0.nupkg --source "Nupack Server" --api-key "your-write-key"
```

### 5. Browse the feed

- Web UI: `http://localhost:5004`
- API service index: `http://localhost:5003/v3/index.json`
- Swagger: `http://localhost:5003/swagger`

## Docker-First Run

Filesystem remains the default compose path:

```bash
docker compose up --build
```

S3-compatible local development uses MinIO through an optional profile:

```bash
PACKAGE_STORAGE_PROVIDER=S3 docker compose --profile s3 up --build
```

Default ports:
- API: `http://localhost:5003`
- Web UI: `http://localhost:5004`
- MinIO API when profile enabled: `http://localhost:9000`
- MinIO console when profile enabled: `http://localhost:9001`

## Package Storage

Preferred configuration shape:

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

The legacy shorthand still works for existing filesystem installs:

```json
{
  "PackageStorage": {
    "BasePath": "data/packages"
  }
}
```

See [package storage configuration](docs/package-storage-configuration.md) for more examples.

Optional write auth for internal deployments can be enabled without changing read endpoints:

```json
{
  "PackageSecurity": {
    "WriteApiKey": "set-via-env-or-secret-store"
  }
}
```

Prefer the environment variable `PackageSecurity__WriteApiKey` or a secret store over committed configuration values.

When `PackageSecurity:WriteApiKey` is empty, write endpoints stay open by default only in the `Development` environment for local reference use. Outside `Development`, missing write auth fails closed with `401 Unauthorized` unless you intentionally set `PackageSecurity:AllowAnonymousWrites` to `true`.

## Architecture at a Glance

- API host: ASP.NET Core Minimal APIs for NuGet V3 endpoints and simple operational routes
- Storage: provider-based package storage with startup scan plus in-memory metadata cache
- UI: separate Razor Pages app that consumes the API over `HttpClient`
- Tests: unit and integration tests in `tests/Nupack.Server.Tests`, including filesystem contract tests and S3/MinIO-ready coverage

See [architecture overview](docs/architecture.md) and [customization guide](docs/customization-guide.md).

## How to Fork and Customize

Common extension paths:
- keep `AddNupackStorage(configuration)` and swap the provider configuration only
- replace `IPackageStorageService` if you want a new storage backend beyond filesystem or S3
- replace `IPackageEndpointAuthorizer` to add auth or policy gates
- replace `IPackageUploadValidator` for custom ingest rules
- replace `IPackageLifecycleHook` for audit, webhook, or metrics hooks
- replace the Web app entirely and keep the API host

If you want to embed these ideas later, the intended direction is to extract reusable packages after the API and docs have stabilized.

## Documentation

- [V3 API guide](docs/v3-api-guide.md)
- [Architecture overview](docs/architecture.md)
- [Customization guide](docs/customization-guide.md)
- [Package storage configuration](docs/package-storage-configuration.md)
- [Community roadmap](docs/roadmap.md)
- [Usage examples](examples/usage-examples.md)

## Releases

The public release surface for the current phase is:
- source code in this repository
- GitHub releases
- Docker images from CI

NuGet package distribution for the server itself is intentionally deferred until the public API shape is stable enough to support embedding.

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md).

The short version:
- keep changes small and easy to review
- update docs when behavior changes
- add or extend tests for protocol-facing or storage-provider work
- prefer clarity over feature count

This repository also follows [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Security

The current release line supports a shared `X-NuGet-ApiKey` for write operations when `PackageSecurity:WriteApiKey` is configured. If you expose this server outside a trusted environment, configure a write key or explicitly opt in to anonymous writes with `PackageSecurity:AllowAnonymousWrites`, and still use TLS, network controls, and stronger authentication layers where appropriate.

See [SECURITY.md](SECURITY.md) for the current policy and deployment guidance.

## Roadmap

The roadmap is release-gated, beginning with a trustworthy `0.1` deployment and then package integrity, durable operations, and stable extension seams.

See [docs/roadmap.md](docs/roadmap.md) for details.

## License

MIT
