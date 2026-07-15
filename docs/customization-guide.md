# Customization Guide

Nupack Server is intentionally small so it can be forked and reshaped.

## Official Shape

- `src/Nupack.Server.Api` is the protocol host
- `src/Nupack.Server.Web` is the only supported UI and is exposed on port `5004` by the container
- storage is now split into repo-local provider projects:
  - `src/Nupack.Server.Storage`
  - `src/Nupack.Server.Storage.FileSystem`
  - `src/Nupack.Server.Storage.S3`

## Main Extension Seams

### Replace or add storage providers

The storage seam is `IPackageStorageService`.

The API host composes storage through:

```csharp
builder.Services.AddNupackStorage(builder.Configuration);
```

Built-in providers today:
- `FileSystem`
- `S3` for AWS S3, MinIO, and compatible endpoints

Current provider selection lives under `PackageStorage`.

### Add request auth or policy gates

The endpoint auth seam is `IPackageEndpointAuthorizer`.

It runs before:
- `PUT /v3/push`
- `DELETE /v3/delete/{id}/{version}`

The built-in default is `HeaderApiKeyPackageEndpointAuthorizer`.

Behavior of the built-in default:
- upload resolves `PackageSecurity:PublishApiKey`; delete resolves `PackageSecurity:DeleteApiKey`
- `PackageSecurity:WriteApiKey` is a 0.x compatibility fallback when the applicable operation-specific key is empty
- if no applicable operation-specific or legacy key exists in `Development`, that write operation remains open for local reference use
- if no applicable key exists outside `Development`, that operation is denied unless `PackageSecurity:AllowAnonymousWrites` is `true`
- when a key is resolved, the request must send it as `X-NuGet-ApiKey`
- search, read, and download endpoints remain anonymous

If your fork needs a different policy, replace the registration with your own implementation and return `PackageAuthorizationResult.Deny(...)` when a request should be blocked.

`AllowAnonymousPackageEndpointAuthorizer` remains available if a fork wants to force all writes open explicitly.

### Add upload validation rules

The upload validation seam is `IPackageUploadValidator`.

The default implementation is `DefaultPackageUploadValidator`, which only enforces the minimal built-in rules:
- package file must exist
- package file must end with `.nupkg`

Forks can replace this to add:
- package signature checks
- package naming conventions
- file size or retention policies
- organization-specific metadata rules

### Observe lifecycle events

The lifecycle seam is `IPackageLifecycleHook`.

The default implementation is `NullPackageLifecycleHook`.

Use it when you want to:
- emit audit logs
- call webhooks
- publish metrics
- trigger downstream indexing

### Replace the UI

The Web app is just a thin HTTP client over the API host. You can:
- restyle the Razor Pages app
- replace it with your own frontend
- remove it completely and expose only the API host

## Example: Overriding Hooks or Storage

```csharp
builder.Services.AddScoped<IPackageEndpointAuthorizer, MyPackageEndpointAuthorizer>();
builder.Services.AddScoped<IPackageUploadValidator, MyPackageUploadValidator>();
builder.Services.AddScoped<IPackageLifecycleHook, MyPackageLifecycleHook>();
builder.Services.AddSingleton<IPackageStorageService, MyCustomStorageService>();
```

These are regular DI registrations. The last registration wins, so a fork can override the defaults without rewriting the API host.

## Good First Fork Changes

- add a new object-storage provider beside `S3`
- add proxy-backed or tenant-specific auth through `IPackageEndpointAuthorizer`
- add package signature validation through `IPackageUploadValidator`
- add webhook or audit logging through `IPackageLifecycleHook`
- add a package details page in the Web app
