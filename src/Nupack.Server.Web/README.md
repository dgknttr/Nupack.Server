# Nupack Server Web UI

`src/Nupack.Server.Web` is the **official user-facing UI** for the Nupack Server repository.

It is a small Razor Pages app that talks to the API host over HTTP and focuses on the current supported flows:
- browse packages
- search packages
- upload packages
- copy install commands

This UI is intentionally simple. It is meant to be easy to fork or replace.

## Current Scope

Supported in the Web UI today:
- home page package overview
- search page with prerelease toggle
- upload page for `.nupkg` files
- client configuration guidance

Not shipped yet:
- package details page
- download analytics
- built-in auth UX
- admin workflows

## Running Locally

```bash
dotnet run --project src/Nupack.Server.Api --urls "http://localhost:5003"
dotnet run --project src/Nupack.Server.Web --urls "http://localhost:5004"
```

The Web app reads `NuGetServer:BaseUrl` from configuration and defaults to `http://localhost:5003`.

## Customization Notes

This app is a thin client over the API host. The easiest ways to customize it are:
- change branding settings
- replace page markup and styles
- add your own auth workflow in front of uploads
- swap it out entirely and keep the API host intact

## Why This UI Is Official

The API-hosted `/ui` and `/frontend` demo routes were removed. This separate `Nupack.Server.Web` application, exposed on port `5004` by the container, is the only supported UI.
