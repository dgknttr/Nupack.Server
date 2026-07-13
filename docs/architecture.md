# Architecture Overview

Nupack Server uses a small, pragmatic split rather than a deep platform architecture.

## Runtime Shape

### API Host

`src/Nupack.Server.Api` contains:
- NuGet V3 endpoints
- push and delete endpoints
- health endpoint
- local file-system storage wiring
- Swagger for the current HTTP surface

### Storage Layer

The default storage implementation is `FileSystemPackageStorageService`.

It is responsible for:
- storing `.nupkg` files
- reading metadata from package manifests
- caching package metadata in memory at startup
- serving package content streams back to the API layer

### Web UI

`src/Nupack.Server.Web` is the official UI. It is a thin Razor Pages client over the API host and currently focuses on browse, search, upload, and install-command flows.

## Request Flow

Typical flow for a package search:
1. Web UI or client calls the API host.
2. API host delegates to `IV3PackageService`.
3. `IV3PackageService` reads package metadata from the storage abstraction.
4. The API host returns protocol-shaped JSON.

Typical flow for package push:
1. Client uploads a `.nupkg` file.
2. API host validates the upload request.
3. Storage service extracts metadata and writes the package to disk.
4. The in-memory cache is updated.

## User Interface

The API host serves the NuGet V3 protocol, Swagger, and health endpoints. The separate `Nupack.Server.Web` Razor Pages application is the only supported UI and is exposed on port `5004` by the container.

## Extension Seams

Today the clearest extension seams are:
- `IPackageStorageService`
- `IV3PackageService`
- configuration-bound options for storage and branding

Future phases may extract these seams into reusable packages once the host behavior and docs are stable.
