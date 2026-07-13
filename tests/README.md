# Tests

The automated suite is in `Nupack.Server.Tests` and targets .NET 9.

```bash
dotnet test Nupack.Server.sln
```

The suite covers service behavior, filesystem and S3 storage, the NuGet V3 protocol, write authorization, and Web UI smoke flows. S3 protocol tests run against MinIO in CI and are enabled by the `NUPACK_S3_TESTS__*` environment variables defined in `.github/workflows/ci.yml`.

`Fixtures/Consumer` is a real package consumer used by the CLI smoke job. The sample package pushed during that job is `test/TestPackage.1.0.0.nupkg`.

To collect coverage locally:

```bash
dotnet test Nupack.Server.sln --collect:"XPlat Code Coverage"
```

Keep tests deterministic and isolated. Prefer in-process integration tests for protocol behavior; add a CLI smoke step only when behavior must be verified through the real `dotnet nuget` client.
