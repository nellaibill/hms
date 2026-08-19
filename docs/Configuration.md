# Configuration

## Purpose
This document explains how configuration and environment variables are structured and managed across environments, so adding or changing configuration follows a known pattern.

## Scope
Covers configuration sources, environment variable reference, and the distinction between secrets and non-secret config.

**Out of scope:** the deployment pipeline itself (see [Deployment.md](Deployment.md)).

## When to Update This Document
- A new configuration value or environment variable is introduced.
- A new environment (e.g., staging) is added.
- The secrets management approach changes.

## Recommended Sections
- Overview
- Configuration Sources
- Environment Variables Reference
- Environment-Specific Configuration (dev / staging / production)
- Secrets Handling
- Adding New Configuration

---

## Overview
_To be documented._

## Configuration Sources
_To be documented._

## Environment Variables Reference
_To be documented._

## Environment-Specific Configuration
_To be documented._

## Secrets Handling

**Local development** — never commit a real secret to `appsettings.Development.json`. The
following keys are intentionally absent from that file and must be supplied via
[.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) instead,
which ASP.NET Core loads automatically in the Development environment and which lives
outside the repo entirely (`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows,
`~/.microsoft/usersecrets/<id>/secrets.json` on Linux/macOS):

```bash
cd backend/src/HMS.Api
dotnet user-secrets set "Jwt:SigningKey" "<any long random string, dev-only>"
dotnet user-secrets set "SuperAdminSeed:Password" "<a password meeting the hospital seed policy>"
dotnet user-secrets set "PlatformAdminSeed:Password" "<a password for the seeded Platform Admin>"
```

The project's `<UserSecretsId>` is already set in `HMS.Api.csproj` — a fresh clone only
needs the three `dotnet user-secrets set` commands above before `dotnet run` will start
(each of these throws `InvalidOperationException` at startup if missing — by design, so a
missing secret fails loudly rather than silently running with an empty/default value).
Connection strings in `appsettings.Development.json` (`Default`/`Platform`/`PlatformAdmin`)
are intentionally left as plaintext local-only Postgres credentials, not treated as secrets —
they only ever point at a developer's own local database.

**Staging / Production** — `appsettings.json` ships the same keys as empty strings
(`Jwt:SigningKey`, `SuperAdminSeed:Password`, `PlatformAdminSeed:Password`,
`ConnectionStrings:Default/Platform/PlatformAdmin`), which the ASP.NET Core configuration
system expects to be overridden by environment variables at deploy time, using the standard
`Section__Key` double-underscore delimiter (e.g. `Jwt__SigningKey`,
`SuperAdminSeed__Password`, `ConnectionStrings__Default`). **No cloud secrets-manager
integration (Azure Key Vault, AWS Secrets Manager, etc.) exists yet** — this app has no
committed-to hosting target as of this writing (see [Deployment.md](Deployment.md)), so
wiring one is deliberately deferred rather than built speculatively against an unconfirmed
target. Whoever sets up the real production environment must supply these via that
environment's own secret-injection mechanism (e.g. a platform's env-var/secret UI, or a
proper secrets-manager integration once a hosting target is chosen) — plain env vars on the
host are the minimum acceptable baseline, not the intended end state.

## Adding New Configuration
_To be documented._
