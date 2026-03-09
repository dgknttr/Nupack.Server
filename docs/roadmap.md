# Community Roadmap

This roadmap keeps Nupack Server focused on clarity, correctness, and hackability.

## Phase 1 - Trust Reset

Goals:
- align docs with the current `net9` + NuGet V3 implementation
- document supported vs unsupported behavior honestly
- make local setup independent of developer-global NuGet feeds
- establish the separate Web app as the official UI

Deliverables:
- conservative README and support matrix
- updated contributing and security guidance
- root `NuGet.Config`
- cleanup of broken UI promises such as missing package details flows

## Phase 2 - Protocol Correctness

Goals:
- back protocol claims with tests
- close obvious gaps in the V3 experience
- keep CLI and integration smoke tests in CI

Deliverables:
- real `.nuspec` extraction
- seeded integration tests for search, push, delete, manifest, and versions
- CI smoke coverage for add-source, push, and restore

## Phase 3 - Extensibility

Goals:
- document and stabilize the main extension seams
- make the repository easier to fork into custom deployments

Deliverables:
- storage customization guidance
- auth hook guidance
- clear boundaries between API host, Web UI, and future reusable pieces

## Phase 4 - Package Extraction

Goals:
- extract reusable hosting and storage packages only after docs and protocol behavior are stable

Intended direction:
- `Nupack.Server.Core`
- `Nupack.Server.Hosting.AspNetCore`
- `Nupack.Server.Storage.FileSystem`
- sample host and sample UI apps

## Not on the Near-Term Roadmap

- enterprise positioning
- built-in auth productization
- multi-tenant hosting
- download analytics
- unlisting
- broad storage-provider matrix in the main branch
