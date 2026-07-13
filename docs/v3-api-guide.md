# Nupack Server V3 API Guide

This guide describes the current supported V3 surface of Nupack Server.

## Positioning

Nupack Server is a NuGet V3 server reference implementation. It is intended for self-hosted feeds, learning, and forks, not for enterprise package-platform claims.

## Base URL

Use the API host base URL plus the following endpoints.

Example local base URL:

```text
http://localhost:5003
```

## Supported Endpoints

### Service index

```text
GET /v3/index.json
```

### Search

```text
GET /v3/search?q=Example&skip=0&take=20&prerelease=false
```

### Flat container versions

```text
GET /v3-flatcontainer/{id}/index.json
```

### Package download

```text
GET /v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg
```

### Nuspec manifest

```text
GET /v3-flatcontainer/{id}/{version}/{id}.nuspec
```

### Registration index

```text
GET /v3/registrations/{id}/index.json
```

### Registration page

```text
GET /v3/registrations/{id}/page/{lower}/{upper}.json
```

### Registration leaf

```text
GET /v3/registrations/{id}/{version}.json
```

### Push

```text
PUT /v3/push
```

Outside `Development`, configure `PackageSecurity:PublishApiKey` and send it as `X-NuGet-ApiKey`, or explicitly opt in to anonymous writes with `PackageSecurity:AllowAnonymousWrites=true`.

### Delete

```text
DELETE /v3/delete/{id}/{version}
```

Outside `Development`, configure `PackageSecurity:DeleteApiKey` and send it as `X-NuGet-ApiKey`, or explicitly opt in to anonymous writes with `PackageSecurity:AllowAnonymousWrites=true`.

### Health

```text
GET /health
```

## Support Matrix

| Capability | Status |
| --- | --- |
| Service index | Supported |
| Search | Supported |
| Flat container versions | Supported |
| Package download | Supported |
| Registration index/page/leaf | Supported |
| Push | Supported |
| Delete | Supported |
| Health | Supported |
| Nuspec exactness | Supported |
| SemVer and prerelease handling | Partial |
| Authentication | Partial |
| Unlist | Not supported |
| Download stats | Not supported |

## Notes

- The separate Web app is the official UI.
- The API host does not serve a UI. Use the separate Web app on port `5004`.
- Search, read, and download endpoints remain anonymous.
- Built-in write auth is intentionally minimal: `X-NuGet-ApiKey` uses separate publish and delete credentials. `PackageSecurity:WriteApiKey` is a compatibility-only `0.x` fallback for both operations. Missing applicable credentials are open by default only in `Development`.
- Rate limiting and advanced admin workflows are not built in.
- Swagger is available at `/swagger` for the current live surface.
