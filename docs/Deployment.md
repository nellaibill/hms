# Deployment

## Purpose
This document explains how the application is built and deployed. The app ships as three
Docker images (`postgres`, `HMS.Api`, and an nginx-served frontend build) orchestrated by
the root `docker-compose.yml` — see that file plus `.env.example` for the runnable stack;
no separate orchestration platform (Kubernetes, etc.) is used at this MVP stage.

## Scope
Covers environments, build process, deployment process, database migration deployment, and rollback strategy.

**Out of scope:** the internal logic of CI scripts (see `cicd/scripts/`) and general configuration management (see [Configuration.md](Configuration.md)).

## When to Update This Document
- The deployment process or target environment changes.
- A new environment is introduced.
- The rollback strategy changes.

## Recommended Sections
- Overview
- Environments
- Build Process
- Deployment Process
- Database Migration Deployment
- Rollback Strategy
- Manual Deployment Steps (MVP fallback)

---

## Overview
_To be documented._

## Environments
_To be documented._

## Build Process
_To be documented._

## Deployment Process
_To be documented._

## Database Migration Deployment
Per [DatabaseArchitecture.md](DatabaseArchitecture.md)'s Migration Strategy: pending
migrations are applied as an explicit, logged step before the new application version
begins serving traffic — not automatically on every startup.

`dotnet HMS.Api.dll migrate` runs that step: it migrates the Platform database, seeds the
Platform Admin account, and — only when `Bootstrap:SeedLegacyTenant` resolves to `true`
(default) — also migrates and seeds the legacy tenant (`ConnectionStrings:Default`). It
then exits (code `0` on success, non-zero on failure) without starting Kestrel, so it's
safe to run as a one-off step ahead of starting the real app process, in any
`ASPNETCORE_ENVIRONMENT`. Set `Bootstrap__SeedLegacyTenant=false` for a real multi-tenant
production deployment — hospitals should be provisioned through the Register Hospital
flow, not the legacy dev/QA seed path.

This is the same migrate+seed logic `ASPNETCORE_ENVIRONMENT=Development` already runs
automatically on a plain `dotnet run`, for local convenience — `migrate` is the explicit
counterpart meant for a deploy pipeline or container entrypoint, usable in Production too.
`install-api-service.ps1` (Manual Deployment Steps, below) runs this automatically before
installing/starting the service, with the service itself running as
`ASPNETCORE_ENVIRONMENT=Production` — migrations are no longer coupled to running the whole
app as Development, which also unintentionally left Swagger UI reachable (Swagger is gated
on `IsDevelopment()`).

**Additional hospital tenants are not covered by `migrate`.** Any hospital provisioned
through the Register Hospital flow (i.e. every tenant other than the legacy one above) has
its own physical database, and `dotnet HMS.Api.dll migrate` never touches it. After
restarting the API on a new version that adds migrations, go to **Platform admin →
Hospitals** and click **Migrate** on each such hospital — this calls `HospitalsController`'s
PlatformSuperAdmin-only migrate action (`platformHospitalsApi.migrateHospital(id)`,
[HospitalTable.tsx](../frontend/web/src/features/platformHospitals/components/HospitalTable.tsx)),
which applies that tenant's pending EF Core migrations on demand — the operator-triggered
alternative to running `dotnet ef database update` by hand against its connection string.
Do this for every registered hospital, not just one, since each has an independent migration
history.

**Symptom of forgetting this step:** a `Npgsql.PostgresException (0x80004005): 42703: column
"..." does not exist` in the API logs (SqlState `42703`) on a request that touches the new
column/table — not a startup failure, since the app itself starts fine and the *other*
tenants (Platform, legacy) are already migrated. If you see `42703` right after a deploy that
added a migration, this is the first thing to check: migrate the hospital tenant that request
was actually resolved against, via the Migrate button above.

### One-time: tenant-scoped file storage migration (`migrate-tenant-files`)
Per [DecisionLog.md ADR-083](DecisionLog.md): uploaded files (documents, consultant/user
photos, product images, branding logo) are now stored under a per-tenant subfolder rather than
one shared `wwwroot/uploads`/`App_Data/documents` tree. Any file uploaded *before* this change
still needs to be moved into its tenant's own folder (and, for everything except Documents, its
DB path column rewritten to match) — this is a separate, one-time, explicitly-run step, deliberately
**not** part of `migrate` or Development's auto-migrate, since unlike a schema migration it
moves real files and rewrites real rows in each tenant's live database.

Run it once, after `migrate` (so the schema is current) and after the new app version is
deployed:

```bash
# 1. Dry run first — reports per-tenant, per-kind counts (moved / already-migrated /
#    missing source file) without touching any file or row.
dotnet HMS.Api.dll migrate-tenant-files --dry-run

# 2. Review the report, then apply for real.
dotnet HMS.Api.dll migrate-tenant-files
```

Idempotent and safe to re-run — anything already at its new tenant-scoped path is skipped, and
one tenant failing is logged and skipped rather than aborting the whole run. Old files stay
reachable at their old path until a tenant is migrated, so nothing breaks for a tenant that
hasn't been run yet.

## Rollback Strategy
_To be documented._

## Manual Deployment Steps

Native (non-Docker) deployment of a single VPS instance, reachable from outside via its
public IP, with PostgreSQL kept private. Automation lives in
[scripts/deploy/windows/](../scripts/deploy/windows/); this section explains what each piece
does and why. Examples below use `162.35.105.234` as the VPS's public IP and `58158` as the
API's port — substitute your own.

### What was already true before any of this (found by inspection, not assumed)

- **Kestrel binding** — [backend/src/HMS.Api/Properties/launchSettings.json](../backend/src/HMS.Api/Properties/launchSettings.json)
  hardcodes `https://localhost:58157;http://localhost:58158`, but that file only affects
  `dotnet run`/Visual Studio. The actual published app takes its URLs from the
  `ASPNETCORE_URLS` environment variable — already how `docker-compose.yml`/the API
  Dockerfile do it (`ENV ASPNETCORE_URLS=http://+:8080`). **No code change needed** to bind
  externally — just set that env var when running the published app.
- **CORS** — [backend/src/HMS.Api/Configuration/CorsConfiguration.cs](../backend/src/HMS.Api/Configuration/CorsConfiguration.cs)
  reads `Cors:AllowedOrigins` from configuration and fails closed (empty list = allow
  nothing, never `AllowAnyOrigin`). Already override-able via the `Cors__AllowedOrigins__0`
  env var, same convention `docker-compose.yml` already uses. **No code change needed.**
- **Swagger** — `SwaggerConfiguration.UseHmsSwagger` gates it behind `app.Environment.
  IsDevelopment()`. It is **not** reachable when `ASPNETCORE_ENVIRONMENT=Production` (the
  value `install-api-service.ps1` sets — see step 2 below). It was previously reachable on a
  real deployment only because the service ran with `ASPNETCORE_ENVIRONMENT=Development` for
  an unrelated reason (migration convenience); that's fixed now, so this bullet's earlier
  claim ("registered unconditionally... not gated") no longer describes either the code or
  the deployed behavior.
- **Secrets** (`Jwt:SigningKey`, `SuperAdminSeed:Password`, `PlatformAdminSeed:Password`) —
  ship as empty strings in `appsettings.json` and throw at startup if unset (see
  [Configuration.md](Configuration.md)). A Windows Service can't use `dotnet user-secrets`
  (that only resolves for the interactive dev user in Development), so these must be
  supplied as real environment variables on the service — see
  `install-api-service.ps1` below.
- **Frontend API URL** — [frontend/web/src/config/env.ts](../frontend/web/src/config/env.ts)
  reads `import.meta.env.VITE_API_BASE_URL`, falling back to `http://localhost:5000` if
  unset. This is a Vite build-time constant (baked into the JS bundle by `vite build`, per
  the existing comment in `frontend/web/Dockerfile`) — **not** something you can override
  after the fact by editing a running deployment; it must be set before `npm run build`.
  Every API call already resolves against this base plus a `/api/...`-prefixed path (see
  [frontend/shared/constants/routes.ts](../frontend/shared/constants/routes.ts)), so no
  frontend code hardcodes `localhost` beyond that one fallback default.
- **PostgreSQL** — a native install (e.g. via `scripts/setup/windows/install-hms-prereqs.ps1`)
  defaults `listen_addresses` and `pg_hba.conf` to localhost-only connections. As long as no
  firewall rule for 5432 is ever added, it's already private by default — confirmed, not
  assumed (see Testing below).

### 1. Reverse proxy: one public port, not two

Rather than exposing the frontend and API on two separate public ports with a CORS
allowlist between them, [scripts/deploy/windows/nginx-hms-reverse-proxy.conf](../scripts/deploy/windows/nginx-hms-reverse-proxy.conf)
puts a single nginx instance on port 80 in front of both: `/` serves the built React app,
`/api/*` and `/swagger/*` proxy to Kestrel on `127.0.0.1:58158`. This means the browser only
ever talks to `http://<public-ip>/...` — same-origin, so the deployed app doesn't depend on
CORS being configured correctly at all (CORS is still configured below, both because the API
port can optionally be opened directly for testing, and because `AllowAnyOrigin` is never
acceptable per existing project convention).

```powershell
choco install nginx -y
copy C:\hms\scripts\deploy\windows\nginx-hms-reverse-proxy.conf C:\tools\nginx-*\conf\conf.d\hms.conf
# Edit the copied file's `root` line if frontend/web/dist isn't at C:\hms\frontend\web\dist
C:\tools\nginx-*\nginx.exe
```
**Verify:** `curl http://localhost/health` returns `OK`.

### 2. Backend — publish and run as a Windows Service

```powershell
cd C:\hms\backend\src\HMS.Api
dotnet publish -c Release -o C:\hms\backend\publish
```

Then install it as a continuously-running service (NSSM — the native-Windows equivalent of
a systemd unit; no application code changes):

```powershell
cd C:\hms\scripts\deploy\windows
.\install-api-service.ps1 `
  -PublicOrigin "http://162.35.105.234" `
  -JwtSigningKey "<generate one: see below>" `
  -SuperAdminPassword "<...>" `
  -PlatformAdminPassword "<...>"
```
Generate a signing key the same way as local dev setup:
```powershell
$b = New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Fill($b)
[Convert]::ToBase64String($b)
```

**Why a Windows Service and not just `dotnet run` in a terminal:** the requirement is that
the API keeps running after you log off / the terminal closes / the VM reboots — the same
guarantee `restart: unless-stopped` gives the Docker deployment.

**Verify:**
```powershell
Get-Service HmsApi                      # should show Running
curl http://localhost:58158/health      # from the VPS itself
```

If this deploy adds new migrations and this VPS has hospital tenants beyond the legacy one,
also run the migrate step from [Database Migration Deployment](#database-migration-deployment)
above, then use the Platform admin **Hospitals → Migrate** button per hospital.

### 3. Frontend — build for production against the public origin

```powershell
cd C:\hms\frontend\shared
npm install
npm run build

cd C:\hms\frontend\web
$env:VITE_API_BASE_URL = "http://162.35.105.234"
npm install
npm run build
```
This produces `frontend/web/dist`, which is what nginx (step 1) serves as static files.
**Every** subsequent change to the public IP/domain requires re-running this build — it's
baked in, not read at runtime.

### 4. Firewall — only what actually needs to be public

```powershell
cd C:\hms\scripts\deploy\windows
.\open-deployment-firewall-ports.ps1
# add -ExposeApiPortDirectly only if you also want to curl the API on port 58158 directly,
# bypassing the reverse proxy, for testing — Swagger UI itself will still 404 there too,
# since it's gated on ASPNETCORE_ENVIRONMENT=Production (see step 2 above), not just on
# which port/proxy path reaches it
```
This opens **port 80 only** by default. It never touches port 5432 — PostgreSQL stays
unreachable from outside this machine. If this VPS is hosted on a cloud provider
(AWS/Azure/GCP/etc.), that provider's own Security Group / Network Security Group must also
allow port 80 inbound — Windows Firewall alone isn't sufficient there.

### 5. CORS

Already handled by `install-api-service.ps1` setting `Cors__AllowedOrigins__0` to the
`-PublicOrigin` value (`http://162.35.105.234` in the example above) — the exact origin the
browser sends when the React app (served from that same origin via nginx) calls the API.
Do **not** set this to `*`/`AllowAnyOrigin` — `CorsConfiguration.cs` doesn't support that
mode at all by design (fails closed on an empty list instead).

### 6. If the API is actually running via `dotnet run`, not the published Windows Service

The steps above assume `install-api-service.ps1` (publish + NSSM) is how the API runs. In
practice a VPS can end up running the API a different way instead — e.g. someone ran
`dotnet run` directly in a terminal to get something working quickly, and it just... kept
running that way. Recognize this by `Get-Service HmsApi` returning "Cannot find any service"
while `Get-Process` shows a `dotnet.exe` (the `dotnet run` host) plus a child `HMS.Api.exe`
(the actual app, from `bin\Debug\net10.0\`, not `bin\Release`) — check with:
```powershell
Get-Process | Where-Object { $_.ProcessName -match "dotnet|HMS.Api" } | Select-Object Id, ProcessName, Path, StartTime
```
This mode works for a quick fix, but has three sharp edges the published/NSSM path doesn't:

- **`ASPNETCORE_URLS` gets silently overridden.** `dotnet run` loads
  [launchSettings.json](../backend/src/HMS.Api/Properties/launchSettings.json)'s launch
  profile (you'll see `Using launch settings from ...` in the output), and the profile's own
  `applicationUrl` wins over an ambient `$env:ASPNETCORE_URLS` you set in the same shell —
  Kestrel still binds `localhost` only, so the app works from `curl http://localhost:...` on
  the VM itself but times out (`ERR_CONNECTION_TIMED_OUT`) from anywhere external, even with
  the firewall port open. Fix: pass `--urls` on the command line instead, which *does* take
  precedence — `dotnet run --urls "http://+:58158"` — and confirm the startup log now shows
  `http://[::]:58158` or `http://+:58158`, not `http://localhost:58158`.
- **Port 58158 (or whatever port) needs its own firewall rule**, same as the
  `-ExposeApiPortDirectly` case in step 4 above — `open-deployment-firewall-ports.ps1` was
  written for the NSSM/nginx path and won't have been run for an ad hoc `dotnet run` session.
  Check with `Get-NetFirewallRule -DisplayName "*<port>*"`; add one with
  `New-NetFirewallRule -DisplayName "HMS API <port>" -Direction Inbound -Protocol TCP -LocalPort <port> -Action Allow`
  if it's missing.
- **`ASPNETCORE_ENVIRONMENT` also comes from the launch profile**, not the ambient shell — so
  `$env:ASPNETCORE_ENVIRONMENT` printing empty doesn't mean the app is running as `Production`;
  check the `Hosting environment:` line the app itself prints at startup instead of trusting
  the shell variable.

To pick up a new commit while running this way: stop the process (`Ctrl+C` in its terminal, or
`Stop-Process -Id <dotnet.exe PID> -Force`), `git pull`/`checkout` the target commit, then
`dotnet run --urls "http://+:<port>"` again from `backend/src/HMS.Api` — this recompiles
against the current source and, if `ASPNETCORE_ENVIRONMENT=Development` (the launch profile's
default), auto-runs the Platform + legacy-tenant migration on startup per
[Database Migration Deployment](#database-migration-deployment) above. It will **not** migrate
any real hospital tenant — that still needs the Migrate button, same as the published path.
Treat this mode as an interim/testing state, not a real deployment: migrate to
`install-api-service.ps1` (step 2 above) when convenient, since a `dotnet run` session dies the
moment the terminal/session closes or the VM reboots, with no automatic restart.

## Testing

1. **API from the VPS itself:** `curl http://localhost:58158/health` → `Healthy`.
2. **API via public IP, direct port** (only if `-ExposeApiPortDirectly` was used):
   from another machine, `curl http://162.35.105.234:58158/health`.
3. **API via the reverse proxy, public IP:** from another machine,
   `curl http://162.35.105.234/api/v1/...` (any anonymous endpoint) or
   `curl http://162.35.105.234/health`.
4. **Swagger is NOT reachable externally:** open `http://162.35.105.234/swagger` in a browser
   on another machine and confirm it 404s — `ASPNETCORE_ENVIRONMENT=Production` (step 2
   above) gates Swagger off entirely. If it loads instead, the service isn't actually running
   as Production; check `Get-Service HmsApi`'s environment (`nssm get HmsApi
   AppEnvironmentExtra`) rather than assuming the deploy script ran successfully.
5. **React from another machine:** open `http://162.35.105.234/` in a browser, open
   DevTools → Network before logging in, confirm login requests go to
   `http://162.35.105.234/api/v1/auth/login` (same-origin, no CORS error) and return `200`.
6. **React → API → PostgreSQL end to end:** a successful login (step 5) already proves this
   — `AuthenticationService` can't issue a token without a live database round-trip.
7. **PostgreSQL stays private:** from a *different* machine,
   `Test-NetConnection 162.35.105.234 -Port 5432` should report `TcpTestSucceeded: False`.
   If it succeeds, a firewall rule for 5432 exists somewhere and needs to be removed.
8. **Static assets are actually gzipped** — Chocolatey's stock nginx ships with gzip off,
   which otherwise turns the frontend's ~200 KB gzip build into a ~677 KB raw transfer on
   every load. `curl -sI -H "Accept-Encoding: gzip" http://162.35.105.234/assets/<any .js file
   from dist/assets>` should include `Content-Encoding: gzip`; without it, re-check the
   `gzip on;` block in `nginx-hms-reverse-proxy.conf` was actually picked up (`nginx -s reload`
   after editing, or `nginx -t` to check for a config error first).

If step 5 fails with a CORS error specifically (not a network/connection error), it means
`Cors__AllowedOrigins__0` on the running service doesn't exactly match the origin the
browser sent — check `Get-Content C:\hms\backend\publish\service.err.log` and compare against
the URL bar's scheme+host+port exactly (including the presence/absence of a port number).
