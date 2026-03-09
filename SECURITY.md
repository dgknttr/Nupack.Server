# Security Policy

Nupack Server is currently maintained as a `0.x` NuGet V3 server reference implementation.

## Supported Release Line

Security fixes are applied to the latest `0.x` release line in this repository. Older commits, forks, and experimental branches should be treated as unsupported unless noted otherwise in a release.

## Current Security Model

What the repository does today:
- validates uploaded files as `.nupkg`
- supports an optional shared `X-NuGet-ApiKey` for `push` and `delete` when `PackageSecurity:WriteApiKey` is configured
- limits upload size in the Web UI
- avoids exposing detailed server errors in normal API responses
- ships container and CI security scanning workflows

What the repository does **not** do today:
- built-in user or account authentication
- built-in role or policy management
- built-in rate limiting
- package signature validation
- hardened multi-tenant isolation

Because of that, this server should still be treated as a trusted-environment component unless you add your own controls.

## Recommended Deployment Controls

If you deploy this outside a local or trusted network, add:
- TLS termination
- a secret store or environment variable for `PackageSecurity__WriteApiKey`
- reverse proxy authentication or your own stronger auth layer when appropriate
- network allowlists or VPN boundaries
- logging and monitoring
- filesystem permission hardening
- regular backups of the package store

## Reporting a Vulnerability

Please do not report security vulnerabilities through public issues.

Preferred channels:
1. GitHub Security Advisories
2. Maintainer email or other private contact listed in the repository profile

Please include:
- affected version or commit
- impacted endpoint or behavior
- reproduction steps
- proof of concept if available
- expected impact

## Response Expectations

We aim to:
- acknowledge reports within 3 business days
- communicate whether the report is reproducible
- ship or document a fix path in the next appropriate `0.x` release

## Security Guidance for Contributors

When changing protocol or upload behavior:
- keep validation explicit
- avoid promising security features that do not exist
- prefer safe defaults in docs and samples
- add tests for malformed, unauthorized, or duplicate package flows when relevant
