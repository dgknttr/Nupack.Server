# Tests

The automated suite is in `Nupack.Server.Tests` and targets .NET 9.

```bash
dotnet test Nupack.Server.sln
```

The suite covers service behavior, filesystem and S3 storage, the NuGet V3 protocol, write authorization, and Web UI smoke flows. S3 protocol tests run against MinIO in CI and are enabled by the `NUPACK_S3_TESTS__*` environment variables defined in `.github/workflows/ci.yml`.

`Fixtures/Consumer` is a real package consumer used by the container smoke job. The sample package pushed during that job is `test/TestPackage.1.0.0.nupkg`.

Run the production-image smoke workflow locally with Docker, `dotnet`, and `curl` installed:

```bash
bash tests/smoke/container-smoke.sh
```

It performs real NuGet push and restore operations, restarts the same container with its filesystem volume, then restores through a different empty client cache. Package source mapping pins `TestPackage` exactly to Nupack while allowing framework packs and other dependencies to resolve from NuGet.org; the detailed restore log is checked to prove the sample package came from Nupack. Temporary containers, volumes, caches, and generated credentials are unique per run and cleaned up automatically.

To collect coverage locally:

```bash
dotnet test Nupack.Server.sln --collect:"XPlat Code Coverage"
```

Keep tests deterministic and isolated. Prefer in-process integration tests for protocol behavior; extend the container smoke only when behavior must be verified through the real image and `dotnet nuget` client.
