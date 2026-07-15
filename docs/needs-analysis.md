# Product Needs Analysis

## Decision

Position Nupack Server as a small, readable, self-hosted NuGet V3 server/feed whose primary use case is storing and serving a company's own internal `.nupkg` packages. It implements the server side of the NuGet V3 protocol and works with standard NuGet clients and the standard package format; it is not a replacement for the NuGet client, `.nupkg`, or NuGet itself. Do not position it as a smaller enterprise artifact platform.

The project should win on three qualities:

1. a dependable first deployment for a small .NET team
2. code and extension points that are easy to understand and change
3. honest protocol and operational boundaries

## Target Users and Jobs

### Small .NET teams

They need a company-controlled feed for internal libraries and CI artifacts without adopting a broad artifact-management platform. Their core job is reliable push and restore with minimal administration. In the common trusted-network model, developers and CI jobs restore without credentials while only approved publishers can push or delete packages.

### On-premises, lab, and disconnected environments

They value data ownership, filesystem or S3-compatible storage, deterministic restores, backup, and recovery more than a rich portal.

### Contributors and platform developers

They need a compact NuGet V3 implementation they can study or adapt with custom storage, authorization, validation, and lifecycle behavior.

## External Signals

- The NuGet V3 service index exposes independent capabilities; a feed can remain deliberately small while correctly advertising only the resources it supports. The required core covers package content, publishing, registration, and search: [NuGet Server API overview](https://learn.microsoft.com/en-us/nuget/api/overview) and [service index](https://learn.microsoft.com/en-us/nuget/api/service-index).
- The NuGet publish protocol allows delete to mean unlist, soft delete, or hard delete. Mature feeds tend to protect consumers from destructive deletion, so lifecycle policy is a trust feature rather than UI polish: [push and delete protocol](https://learn.microsoft.com/en-us/nuget/api/package-publish-resource).
- Azure Artifacts emphasizes immutable versions, indexed metadata, package validation, upstream sources, retention, and recovery. These are useful indicators of operator needs, not a feature checklist for this project: [key concepts](https://learn.microsoft.com/en-us/azure/devops/artifacts/artifacts-key-concepts?view=azure-devops).
- BaGetter already offers a larger self-hosted server, symbols, cloud deployment, and mirroring. Sleet offers a mature static-feed model with validation and retention. Nupack Server should not compete by copying both surfaces: [BaGetter](https://github.com/bagetter/BaGetter) and [Sleet](https://github.com/emgarten/Sleet).

Six GitHub stars are a useful discovery signal, but they do not prove production use or identify the missing feature. Roadmap candidates should be validated through issue workflows, deployment feedback, and release/container usage where available.

## Current Strengths

- NuGet V3 service index, search, flat container, registration, push, and delete flows
- real `dotnet nuget` push/restore smoke coverage in CI
- filesystem and S3-compatible package storage
- a separate Web UI and explicit customization seams
- fail-closed write configuration outside Development
- a compact codebase and automated protocol tests

## Highest-Risk Gaps

| Risk | Current behavior | Why it matters | Priority |
| --- | --- | --- | --- |
| Package lifecycle | Delete immediately removes bytes and duplicate protection is a check followed by a write | Retries, races, or mistakes can break consumers | P0/P1 |
| Metadata durability | Each process rebuilds an in-memory catalog by reopening every stored package | Startup cost grows with the feed and instances can disagree | P1/P2 |
| S3 download memory | The whole object is copied into a `MemoryStream` | Large or concurrent downloads can exhaust memory | P1 |
| Credential migration | Publish and delete support separate credentials, while `WriteApiKey` remains a shared 0.x compatibility fallback | Deployments that keep the legacy fallback still grant publishers destructive delete authority | P1 |
| Recovery | Backup, restore, reconciliation, and corruption checks are not executable workflows | Operators cannot prove their feed is recoverable | P1 |
| Release process | There is no tagged 0.x release or documented compatibility policy yet | Operators cannot pin an intentional release or judge upgrade risk | P1 |

## Recently Closed Trust Gaps

Two issues originally identified as P0 are now covered by implementation and regression tests:

- The production image exposes HTTP only, provides matching API and Web health routes, runs as a non-root user, and uses a Docker-managed volume by default. The container smoke gate builds that image, pushes and restores a package, restarts with the same volume, and restores again from an empty client cache.
- `/health/live` reports process liveness without probing dependencies. `/health/ready` and its `/health` compatibility alias probe the selected filesystem or S3-compatible package store, enforce a bounded timeout, and return `503` when storage is unavailable.

These remain release gates rather than roadmap gaps: a regression in either area should block a release.

## Prioritization Rules

Use these rules when accepting feature work:

1. Protocol correctness and restore safety beat UI breadth.
2. Recovery and observability beat additional storage providers.
3. Extension seams beat built-in identity or enterprise workflow features.
4. A feature needs a concrete user workflow and a verification plan.
5. Do not advertise behavior until a real NuGet client or deployment test proves it.

## Access Boundary

Anonymous search, metadata reads, and package downloads are the default product model, not a primary access-control defect. A typical deployment places Nupack on a trusted company network, behind a VPN, or behind a reverse proxy that defines who can reach the feed. This keeps `dotnet restore` simple for developer machines and CI jobs because read credentials do not need to be distributed and rotated.

Publish and delete change repository state and must be authenticated. The `0.1` direction is to give those operations separate credentials so a publisher does not automatically receive destructive delete authority. Deployments that require confidential package contents can enforce authenticated reads at their reverse proxy or through a custom authorization layer; built-in authenticated reads remain optional and are not a `0.1` requirement.

## Discovery Questions

Use GitHub issues or short user interviews to answer these before promoting optional work:

- Is the feed used mainly for private CI artifacts, disconnected restores, or learning/customization?
- Is the feed reachable only from a trusted network/VPN, or does a reverse proxy define the read boundary?
- Is there a demonstrated need for authenticated reads beyond that boundary, and where should those credentials be managed?
- Which people or CI identities need publish access, and who should hold the separate delete credential?
- Are users blocked more often by missing symbols, upstream caching, or lifecycle controls?
- What are the typical package count, versions per ID, largest package size, and concurrent restore load?
- Do deployments use local volumes, AWS S3, MinIO, or another compatible object store?
- What recovery point and recovery time do users expect?

## Success Measures

For the next release line, prefer measures that indicate trust rather than popularity:

- documented container smoke path passes on every release
- push/restore/restart/restore scenario passes for filesystem and S3
- zero known protocol regressions in supported resources
- recovery drill and catalog rebuild complete from documented commands
- issue templates capture deployment type, client version, storage provider, and package scale
- at least three concrete user workflows validate any feature moved out of “Validate Before Scheduling”
