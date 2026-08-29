# Development Guidelines

## Purpose
This document gets a developer from a clean machine to a running local environment, and documents day-to-day development expectations. It exists to reduce onboarding friction, which matters most for the two junior developers on the team.

## Scope
Covers prerequisites, local environment setup, and running each part of the system (web, mobile, backend API, database) locally.

**Out of scope:** git branching/PR process (see [GitWorkflow.md](GitWorkflow.md)) and code style rules (see [CodingStandards.md](CodingStandards.md)).

## When to Update This Document
- Local setup steps change (new dependency, new tool version).
- A new environment variable is required for local development.
- A recurring setup problem is discovered (consider also adding it to [Troubleshooting.md](Troubleshooting.md)).

## Recommended Sections
- Prerequisites
- Local Environment Setup
- Running the Web App
- Running the Mobile App
- Running the Backend API
- Running the Database
- Environment Variables
- Common Development Tasks

---

## Prerequisites

Core tooling needed across the whole stack:

| Tool | Version | Notes |
|---|---|---|
| Git | >= 2.40 | |
| .NET SDK | 10.0.x | matches `<TargetFramework>net10.0` in [Directory.Build.props](../Directory.Build.props) |
| `dotnet-ef` | 10.0.x | `dotnet tool install --global dotnet-ef` — needed for EF Core migrations |
| Node.js | 20.x LTS | matches `node:20-alpine` in [frontend/web/Dockerfile](../frontend/web/Dockerfile) |
| npm | >= 10.x | ships with Node 20; repo uses npm workspaces (`frontend/web`, `frontend/shared`) |
| PostgreSQL | 16.x | matches `postgres:16-alpine` in [docker-compose.yml](../docker-compose.yml) — install natively or run via Docker |
| Docker Engine + Compose plugin | v2.x | required even for native dev: `HMS.IntegrationTests` uses Testcontainers to spin up a throwaway Postgres container, and it's the full stack's actual deployment path |

Optional, only if you're touching the mobile app ([frontend/mobile](../frontend/mobile)):

| Tool | Version | Notes |
|---|---|---|
| Expo CLI | via `npx expo` | no global install required |
| JDK | 17 | required by the Android Gradle build tooling |
| Android Studio + Android SDK | latest | only if building/running the Android app locally rather than using Expo Go |
| Watchman | latest | recommended on macOS/Linux; not required on Windows |

Recommended IDE: Visual Studio 2022 17.12+ (".NET desktop"/"ASP.NET and web development" workload) or VS Code with the C# Dev Kit, ESLint, and Prettier extensions.

**Windows Server:** Docker Desktop isn't the supported path on Server editions — install Docker Engine (Moby) directly instead, which requires enabling the Windows "Containers" feature and a reboot. [scripts/setup/windows/](../scripts/setup/windows/) has two scripts that automate all of the above:
- `install-hms-prereqs.ps1` — Git, .NET 10 SDK, `dotnet-ef`, Node 20 LTS, PostgreSQL 16, VS Code (via Chocolatey).
- `install-docker-engine.ps1` — Docker Engine, run separately since it forces a reboot.

Both must be run from an elevated (Administrator) PowerShell.

## Local Environment Setup
_To be documented._

## Running the Web App
_To be documented._

## Running the Mobile App
_To be documented._

## Running the Backend API
_To be documented._

## Running the Database
_To be documented._

## Environment Variables
_To be documented._

## Common Development Tasks
_To be documented._
