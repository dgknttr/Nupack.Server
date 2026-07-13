# Contributing to Nupack Server

Thanks for helping with Nupack Server.

This repository is maintained as a small, fork-friendly NuGet V3 server reference implementation. The best contributions improve one or more of these qualities:
- protocol correctness
- documentation honesty
- onboarding speed
- code clarity
- customization friendliness

## Before You Start

Please read:
- [README.md](README.md)
- [docs/v3-api-guide.md](docs/v3-api-guide.md)
- [docs/roadmap.md](docs/roadmap.md)

## Local Setup

### Prerequisites
- .NET 9 SDK
- Git
- Docker (optional)

### Run the solution

```bash
dotnet restore
dotnet build

dotnet run --project src/Nupack.Server.Api --urls "http://localhost:5003"
dotnet run --project src/Nupack.Server.Web --urls "http://localhost:5004"
```

### Run tests

```bash
dotnet test
```

The repository includes a root `NuGet.Config` so restore and test runs do not depend on your machine-wide feeds.

## What We Value in Pull Requests

Strong pull requests usually do at least one of these:
- make a supported endpoint more correct
- remove drift between docs and code
- improve contributor onboarding
- add tests around a real protocol scenario
- simplify a customization path

## Contribution Rules

- Keep PRs focused. Small, composable changes are easier to review.
- If behavior changes, update the docs in the same PR.
- If a protocol-facing endpoint changes, add or update tests.
- Do not market unsupported features as supported.
- Document the separate `Nupack.Server.Web` app on port `5004` as the only supported UI; the API-hosted `/ui` and `/frontend` demos were removed.

## Branch and Commit Guidance

Suggested branch names:
- `docs/...`
- `fix/...`
- `test/...`
- `refactor/...`
- `feature/...`

Commit messages do not need a strict convention, but they should describe the change clearly.

## Scope Guidance

Good issues for this repo:
- V3 endpoint correctness
- API documentation and examples
- storage path handling
- contributor experience
- test coverage around restore/push/download/search flows

Usually out of scope unless discussed first:
- enterprise feature sets
- multi-tenant behavior
- built-in auth productization
- package extraction and embedding APIs

## Reporting Bugs

When opening a bug, include:
- what you tried
- expected behavior
- actual behavior
- environment details
- reproduction steps or a failing sample package when possible

## Security Reports

Please do not report vulnerabilities in public issues. Use the process in [SECURITY.md](SECURITY.md).

## Recognition

Contributors are credited through release notes and repository history. Thanks for helping keep the project understandable and useful.
