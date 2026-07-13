# Roadmap

Nupack Server is developed as a small, hackable NuGet V3 feed for company-owned internal `.nupkg` packages. It is a server used by standard NuGet clients, not a replacement for the client or package format. Search, read, and download are anonymous by default; state-changing operations are authenticated. Milestones are gated by observable outcomes rather than dates. See the [needs analysis](needs-analysis.md) for the reasoning behind these priorities.

## 0.1 - Trustworthy First Run

Goal: a new user can deploy the documented configuration and prove that push, restore, restart, and recovery work.

- make the container topology and health checks match the processes that actually run
- test the published container, not only source-hosted applications
- add readiness checks for the configured storage provider
- verify push and restore with supported `dotnet` client versions
- separate publish and delete credentials so publishers do not automatically receive destructive authority
- remove or explicitly deprecate the API-hosted legacy UIs
- publish an honest support matrix and first tagged prerelease

Exit gate: a clean machine can follow one documented path, push a fixture, restore it, restart the service, restore it again, and observe a meaningful readiness signal.

## 0.2 - Package Integrity and Lifecycle

Goal: package content remains predictable under retries, concurrent publishers, and operator mistakes.

- validate archive integrity, package ID, version, and canonical path before storing bytes
- make writes atomic and reject duplicate ID/version races in filesystem and S3 storage
- record content hashes and test download integrity
- define immutable version semantics
- prefer unlist/tombstone and explicit purge over immediate hard delete
- add backup, restore, export, and store-validation guidance
- stream S3 downloads instead of buffering whole packages in memory

Exit gate: lifecycle behavior has protocol tests, failure-injection tests, and documented recovery steps for both storage providers.

## 0.3 - Durable Catalog and Operations

Goal: startup and search cost no longer grow by reopening every package, and operators can diagnose failures.

- introduce a storage-independent metadata catalog contract
- ship a simple default catalog with schema migrations and a rebuild command
- reconcile catalog entries with package bytes after interrupted operations
- expose structured health, metrics, and audit events
- document supported single-instance behavior before considering multi-instance coordination
- add rate-limit hooks and reverse-proxy authentication examples

Exit gate: startup does not read every `.nupkg`, catalog rebuild is tested, and operational signals distinguish API health from storage health.

## 0.4 - Stable Extension Surface

Goal: teams can fork or extend the server without editing protocol code.

- stabilize storage, authorization, validation, and lifecycle contracts
- extract reusable hosting components only after their behavior is proven
- provide one reference customization for authentication and one for lifecycle policy
- add compatibility tests for external providers

Exit gate: a sample extension builds outside the main host and survives a documented upgrade.

## Validate Before Scheduling

These features solve real problems but would broaden the product substantially. Open an issue with a concrete workflow before implementation:

- upstream NuGet caching or mirroring
- symbol package publishing
- authenticated reads and per-user read permissions (optional; not required for `0.1`)
- retention rules and download statistics
- multi-instance writers

## Explicit Non-Goals

- replacing the NuGet client or protocol
- supporting npm, Maven, PyPI, or other package ecosystems
- built-in identity-provider or multi-tenant product features
- enterprise availability or compliance claims in the `0.x` line
