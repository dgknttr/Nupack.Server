# Trustworthy First Run Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver a credible `0.1` foundation for an internal NuGet feed with anonymous restore, separately protected publishing and deletion, meaningful readiness, and a tested container restart workflow.

**Architecture:** Keep reads anonymous and keep the existing NuGet V3 surface. Add narrowly scoped write credentials, provider-backed readiness checks, and an HTTP-only two-process convenience container whose real push/restore/restart behavior is exercised in CI. Preserve the compact single-node architecture and avoid identity, database, proxy-cache, or multi-tenant features.

**Tech Stack:** .NET 9 minimal APIs, ASP.NET Core health checks, xUnit, FluentAssertions, Moq, Docker, GitHub Actions, Bash.

---

### Task 1: Establish the 0.1 Product Contract and Clean Baseline

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/needs-analysis.md`
- Modify: `docs/roadmap.md`
- Modify: `tests/README.md`
- Modify/Delete: the already identified obsolete test scaffolding and duplicate repository-governance files currently present in the working tree
- Include: `docs/plans/2026-07-13-trustworthy-first-run.md`

**Step 1: Audit the existing dirty diff**

Run: `git diff --check && git status --short`

Expected: only the cleanup, fixture relocation, product documents, and this plan are changed; no source behavior changes are present.

**Step 2: Correct the product contract**

Make the documents state explicitly:

- internal/company packages are the primary use case
- reads/search/downloads are anonymous by default
- publish and delete are authenticated operations and will use separate credentials
- authenticated reads remain optional and are not a `0.1` requirement
- the product is a NuGet V3 server/feed, not a replacement for the NuGet client or package format

In `docs/needs-analysis.md`, replace the current access-control risk language with the network-boundary model and update discovery questions accordingly. In `docs/roadmap.md`, put credential separation in `0.1` and keep read authentication under “Validate Before Scheduling.”

**Step 3: Verify documentation and paths**

Run:

```bash
git diff --check
rg -n "authenticated reads|anonymous" README.md docs tests .github
rg -n -g '!plans/**' "test-project|test-v3|test-package" README.md docs tests .github || true
```

Expected: no stale deleted paths; anonymous-read guidance is consistent.

**Step 4: Commit**

```bash
git add -A README.md CHANGELOG.md docs tests .github branch-protection.json setup-branch-protection.sh test test-project test-v3 test-package
git commit -m "chore: clean repository and define 0.1 direction"
```

### Task 2: Separate Publish and Delete Credentials

**Files:**
- Modify: `src/Nupack.Server.Api/Models/PackageSecurityOptions.cs`
- Modify: `src/Nupack.Server.Api/Services/HeaderApiKeyPackageEndpointAuthorizer.cs`
- Modify: `tests/Nupack.Server.Tests/Services/HeaderApiKeyPackageEndpointAuthorizerTests.cs`
- Modify: `README.md`
- Modify: `SECURITY.md`
- Modify: `docs/v3-api-guide.md`
- Modify: `docs/openapi.yaml`

**Required behavior:**

- Package reads remain anonymous.
- `PackageSecurity:PublishApiKey` protects push.
- `PackageSecurity:DeleteApiKey` protects delete.
- Legacy `PackageSecurity:WriteApiKey` remains a compatibility fallback for both operations during `0.x`.
- If only `PublishApiKey` is configured, it must not authorize delete.
- If no applicable key exists, the existing Development/`AllowAnonymousWrites` behavior remains.
- Credential comparisons remain fixed-time.

**Step 1: Write failing authorization tests**

Add tests proving:

```csharp
[Fact]
public async Task PublishKey_AuthorizesUpload_ButNotDelete()

[Fact]
public async Task DeleteKey_AuthorizesDelete_ButNotUpload()

[Fact]
public async Task LegacyWriteKey_AuthorizesBothOperations()

[Fact]
public async Task MissingDeleteKey_FailsClosedOutsideDevelopment()
```

**Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter FullyQualifiedName~HeaderApiKeyPackageEndpointAuthorizerTests`

Expected: new tests fail because operation-specific keys do not exist.

**Step 3: Implement minimal option resolution**

Add nullable `PublishApiKey` and `DeleteApiKey` properties. Resolve upload key as `PublishApiKey`, then legacy `WriteApiKey`; resolve delete key as `DeleteApiKey`, then legacy `WriteApiKey`. Do not let `PublishApiKey` fall through to delete.

**Step 4: Run focused and full tests**

Run:

```bash
dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter FullyQualifiedName~HeaderApiKeyPackageEndpointAuthorizerTests
dotnet test Nupack.Server.sln
```

Expected: all tests pass with no new warnings.

**Step 5: Document configuration and compatibility**

Show separate environment variables:

```text
PackageSecurity__PublishApiKey
PackageSecurity__DeleteApiKey
```

Mark `WriteApiKey` as a `0.x` compatibility option, not the recommended configuration.

**Step 6: Commit**

```bash
git add src/Nupack.Server.Api tests/Nupack.Server.Tests README.md SECURITY.md docs
git commit -m "feat: separate package publish and delete credentials"
```

### Task 3: Add Provider-Backed Liveness and Readiness

**Files:**
- Modify: `src/Nupack.Server.Storage/IPackageStorageService.cs`
- Modify: `src/Nupack.Server.Storage.FileSystem/FileSystemPackageStorageService.cs`
- Modify: `src/Nupack.Server.Storage.S3/S3PackageStorageService.cs`
- Create: `src/Nupack.Server.Api/Services/PackageStorageHealthCheck.cs`
- Modify: `src/Nupack.Server.Api/Program.cs`
- Modify: `tests/Nupack.Server.Tests/Storage/FileSystemPackageStorageServiceTests.cs`
- Modify: `tests/Nupack.Server.Tests/Storage/S3PackageStorageServiceTests.cs`
- Create or modify: `tests/Nupack.Server.Tests/Services/PackageStorageHealthCheckTests.cs`
- Modify: `tests/Nupack.Server.Tests/Integration/ApiIntegrationTests.cs`

**Required behavior:**

- `GET /health/live` reports process liveness without touching storage.
- `GET /health/ready` checks the selected storage provider and returns `503` when unavailable.
- Existing `GET /health` remains as a compatibility alias for readiness.
- Filesystem readiness checks that the package directory is reachable and writable without leaving files behind.
- S3 readiness performs a bounded request against the configured bucket and honors cancellation.

**Step 1: Write failing provider and endpoint tests**

Test a writable filesystem, an unavailable path, successful/failing S3 calls, a healthy readiness response, and a failing readiness response. Assert `/health/live` remains `200` when the storage probe fails.

**Step 2: Run focused tests and verify RED**

Run:

```bash
dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter "FullyQualifiedName~Health|FullyQualifiedName~StorageServiceTests"
```

Expected: failures for the missing probe contract and endpoints.

**Step 3: Implement the minimal storage probe**

Add a cancellable probe method to the storage contract. Use a uniquely named zero-byte probe file with guaranteed cleanup for filesystem readiness. Use a `ListObjectsV2` request with `MaxKeys = 1` for S3 readiness. Wrap the selected provider with an ASP.NET Core `IHealthCheck` implementation.

**Step 4: Map health endpoints**

Return a stable JSON shape containing at least `status` and `timestamp`. Readiness failures must use HTTP `503` and must not disclose credentials or raw exception details.

**Step 5: Run focused and full tests**

Run:

```bash
dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter "FullyQualifiedName~Health|FullyQualifiedName~StorageServiceTests"
dotnet test Nupack.Server.sln
```

Expected: all tests pass.

**Step 6: Commit**

```bash
git add src tests
git commit -m "feat: add storage-backed readiness checks"
```

### Task 4: Make the Convenience Container Honest and Reliable

**Files:**
- Modify: `Dockerfile`
- Modify: `docker-compose.yml`
- Modify: `src/Nupack.Server.Web/Program.cs`
- Modify: `tests/Nupack.Server.Tests/Integration/WebSmokeTests.cs`
- Modify: `README.md`

**Required behavior:**

- The image exposes and binds HTTP ports `5003` (API) and `5004` (Web) only; TLS belongs at a reverse proxy.
- The Web host exposes `GET /health/live`.
- The image health check verifies both processes.
- The startup script forwards termination, stops the sibling if either process exits, and exits non-zero when a child fails.
- Runtime health tooling is explicitly installed or otherwise guaranteed to exist.
- Compose does not expose unused HTTPS ports and passes the Web app an API base URL reachable inside the container.
- No default production publish secret is committed.

**Step 1: Write the failing Web liveness test**

Add `WebHealthLive_ReturnsHealthyJson` to `WebSmokeTests` and verify it fails because the route is missing.

**Step 2: Run the test and verify RED**

Run: `dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter FullyQualifiedName~WebHealthLive`

Expected: `404` instead of `200`.

**Step 3: Add Web liveness and simplify container networking**

Map the endpoint, remove internal HTTPS bindings and ports, add robust signal handling to `/app/start.sh`, and make the health check call API readiness plus Web liveness.

**Step 4: Verify source tests and image build**

Run:

```bash
dotnet test Nupack.Server.sln
docker build -t nupack-server:0.1-test .
```

Expected: tests and image build succeed.

**Step 5: Commit**

```bash
git add Dockerfile docker-compose.yml src/Nupack.Server.Web tests/Nupack.Server.Tests README.md
git commit -m "fix: make container startup and health checks reliable"
```

### Task 5: Remove the Legacy API-Hosted UIs

**Files:**
- Modify: `src/Nupack.Server.Api/Program.cs`
- Modify: `tests/Nupack.Server.Tests/Integration/ApiIntegrationTests.cs`
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `docs/v3-api-guide.md`
- Modify: `CHANGELOG.md`

**Required behavior:**

- `/ui` and `/frontend` return `404`.
- The separate `Nupack.Server.Web` application remains the only supported UI.
- Embedded legacy HTML and its helper methods are deleted.
- Root `/` continues returning the V3 service index.

**Step 1: Change integration tests to the desired behavior**

Replace legacy success assertions with:

```csharp
[Theory]
[InlineData("/ui")]
[InlineData("/frontend")]
public async Task LegacyApiHostedUi_ReturnsNotFound(string path)
```

**Step 2: Run tests and verify RED**

Run: `dotnet test tests/Nupack.Server.Tests/Nupack.Server.Tests.csproj --filter FullyQualifiedName~LegacyApiHostedUi`

Expected: tests fail with `200` instead of `404`.

**Step 3: Remove routes and embedded HTML**

Delete only the legacy routes and helpers; do not change protocol endpoints or the separate Web project.

**Step 4: Run full tests**

Run: `dotnet test Nupack.Server.sln`

Expected: all tests pass.

**Step 5: Commit**

```bash
git add src/Nupack.Server.Api tests/Nupack.Server.Tests README.md docs CHANGELOG.md
git commit -m "refactor: remove legacy API-hosted user interfaces"
```

### Task 6: Prove Push, Restore, Restart, and Restore in CI

**Files:**
- Create: `tests/smoke/container-smoke.sh`
- Modify: `.github/workflows/ci.yml`
- Modify: `tests/README.md`
- Modify: `README.md`
- Modify: `docs/ci-cd.md`

**Required behavior:**

- Build the actual Dockerfile.
- Start the image with filesystem persistence and explicit publish/delete keys.
- Wait for API readiness and Web liveness.
- Push `test/TestPackage.1.0.0.nupkg` through the real `dotnet nuget` client.
- Restore `tests/Fixtures/Consumer/Consumer.csproj` through the feed.
- Restart the same container against the same persisted data.
- Clear the consumer package cache and restore again.
- Always clean up containers, volumes/temp directories, and generated NuGet configuration.
- Run as a required CI job before publishing the image.

**Step 1: Write the smoke script with fail-fast assertions**

Use `set -euo pipefail`, a cleanup trap, unique resource names, bounded health polling, and explicit diagnostics on failure. Do not duplicate protocol assertions already covered by xUnit.

**Step 2: Verify the script catches a missing image/server**

Run the script once against an intentionally invalid image override or otherwise demonstrate the expected fail-fast path before the successful run.

Expected: non-zero exit with useful diagnostics.

**Step 3: Run the complete smoke workflow**

Run: `bash tests/smoke/container-smoke.sh`

Expected: push succeeds, both restores succeed, and cleanup completes.

**Step 4: Wire the script into CI**

Add a `container-smoke` job after unit/S3 tests and make image publishing depend on it. Remove redundant older CLI smoke steps if the new job fully supersedes them.

**Step 5: Run final verification**

Run:

```bash
dotnet test Nupack.Server.sln --configuration Release
bash tests/smoke/container-smoke.sh
git diff --check
```

Expected: all checks pass.

**Step 6: Commit**

```bash
git add tests/smoke .github/workflows/ci.yml README.md tests/README.md docs/ci-cd.md
git commit -m "test: verify container push restore and restart workflow"
```

### Final Review

After all six tasks pass their individual specification and quality reviews:

1. Dispatch a fresh reviewer over the full branch diff from `main`.
2. Run `dotnet test Nupack.Server.sln --configuration Release`.
3. Run `bash tests/smoke/container-smoke.sh`.
4. Run `git diff --check main...HEAD`.
5. Use the `finishing-a-development-branch` workflow to offer merge, PR, keep, or discard options.
