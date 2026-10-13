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

> **Prerequisite (pgvector):** from the `AddDocumentChunks` Documents migration onward, every
> Postgres server needs the pgvector extension installed and enabled by a superuser in each
> tenant database and `template1`, or tenant migration fails at startup. See
> [DocumentManagement.md → pgvector Setup](modules/Documents/DocumentManagement.md#pgvector-setup).
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

### Tenant file storage migration (`migrate-tenant-files`)
Per [DecisionLog.md ADR-087](DecisionLog.md): every file a hospital uploads now lives under that
hospital's own `Tenant/{tenantId}` folder:

```
wwwroot/uploads/Tenant/{tenantId}/branding|users|consultants|products/…   (public images)
App_Data/Tenant/{tenantId}/documents/…                                    (private documents)
```

Files saved in either older layout (the original shared `wwwroot/uploads/{kind}` and
`App_Data/documents` trees, or ADR-083's `uploads/{kind}/{tenantId}` and
`App_Data/documents/{tenantId}`) need to be moved, and for everything except Documents their DB
path column rewritten. This is a separate, explicitly-run step, deliberately **not** part of
`migrate` or Development's auto-migrate, since unlike a schema migration it moves real files and
rewrites real rows in each tenant's live database.

Run it **right after** `migrate` and deploying the version that includes ADR-087 — until it runs,
that tenant's existing files are missing from Settings → Backup's documents & images zip:

```bash
# 1. Dry run first — reports per-tenant, per-kind counts (to move / already migrated /
#    missing source file) without touching any file or row.
dotnet HMS.Api.dll migrate-tenant-files --dry-run

# 2. Review the report, then apply for real.
dotnet HMS.Api.dll migrate-tenant-files
```

Idempotent and safe to re-run — anything already in the current layout is skipped, and one
tenant failing is logged and skipped rather than aborting the whole run. Images keep working at
their old path until the run (their DB column still points there), and documents are read from
the ADR-083 folder as a fallback, so nothing breaks in between. Afterwards, the tenant's own
empty ADR-083 folders are removed; anything left in the old shared folders belongs to no
database row and can be reviewed and deleted by hand.

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
  Dockerfile do it (`ENV ASPNETCORE_URLS=http://+:8080`). On a VPS the published app binds
  **localhost only** (`install-api-service.ps1` sets `ASPNETCORE_URLS=http://localhost:58158`):
  nginx on the same machine is the only caller, and the API's own port speaks plain HTTP, so
  it must not be reachable from outside (ADR-092).
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
  the deployed behavior. A server that does run in Development mode (the `dotnet run` case in
  step 6) can still switch it off with `Swagger__Enabled=false`, and the nginx config only
  proxies `/swagger/` for a browser on the VPS itself.
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
  **The same requirement applies to `npm run dev`/`npx vite --host`, not just `npm run
  build`** — Vite resolves `VITE_API_BASE_URL` from the shell env at dev-server-start time
  too. Forgetting it doesn't just point the app at the wrong host; a browser on a real
  machine, loaded from the VM's public IP, treats a plain-HTTP request back to
  `localhost:<port>` as crossing into a more-private address space and blocks it outright
  under Chrome's Private Network Access policy (`... has been blocked by CORS policy: The
  request client is not a secure context and the resource is in more-private address space
  'loopback'`) — this looks like a CORS/network problem but is actually just the missing env
  var, same fix either way: set `VITE_API_BASE_URL` to the VM's public IP/origin before
  starting the dev server, then restart it (not just reload the page — Vite doesn't pick up
  a changed shell env on its own).
- **PostgreSQL** — a native install (e.g. via `scripts/setup/windows/install-hms-prereqs.ps1`)
  defaults `listen_addresses` and `pg_hba.conf` to localhost-only connections. As long as no
  firewall rule for 5432 is ever added, it's already private by default — confirmed, not
  assumed (see Testing below).

### 1. Reverse proxy: one public port, not two

Rather than exposing the frontend and API on two separate public ports with a CORS
allowlist between them, [scripts/deploy/windows/nginx-hms-reverse-proxy.conf](../scripts/deploy/windows/nginx-hms-reverse-proxy.conf)
puts a single nginx instance on port 80 in front of both: `/` serves the built React app,
`/api/*` proxies to Kestrel on `127.0.0.1:58158` (and `/swagger/*` too, but only for a
browser on the VPS itself). This means the browser only ever talks to
`http://<public-ip>/...` — same-origin, so the deployed app doesn't depend on CORS being
configured correctly at all (CORS is still configured below, because `AllowAnyOrigin` is
never acceptable per existing project convention).

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
The service listens on `localhost:58158` only. `-ExposeApiPortDirectly` binds every interface
instead; leave it off unless something outside this machine must call the API on that port.

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
Or run [scripts/deploy/windows/build-frontend.ps1](../scripts/deploy/windows/build-frontend.ps1)
`-ApiBaseUrl "http://162.35.105.234"`, which does the same in the right order (shared before
web) and also clears Vite's dev pre-bundle cache.

This produces `frontend/web/dist`, which is what nginx (step 1) serves as static files.
**Don't serve the app with `npm run dev` on a server.** The Vite dev server compiles every
module per request (measured on staging: 75 requests / 4.4 MB for the login page alone,
8–19 s page loads, vs 20 requests / 0.8 MB for the production build), exposes the
TypeScript source, and serves a stale `@hms/shared` pre-bundle after shared changes.
**Every** subsequent change to the public IP/domain requires re-running this build — it's
baked in, not read at runtime.

### 4. Firewall — only what actually needs to be public

```powershell
cd C:\hms\scripts\deploy\windows
.\open-deployment-firewall-ports.ps1
# -CloseApiPort removes any inbound rule that opens the API's own port (58158)
# -ExposeApiPortDirectly opens it instead, for testing; the API must then also be installed
#  with install-api-service.ps1 -ExposeApiPortDirectly, or nothing listens there
```
This opens **port 80 only** by default. It never touches port 5432 — PostgreSQL stays
unreachable from outside this machine. The API's port stays closed as well: if a rule for it
already exists, the script names it and `-CloseApiPort` removes it. An open 58158 serves the
whole API over plain HTTP, without nginx's TLS in front. If this VPS is hosted on a cloud provider
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
This mode works for a quick fix, but has four sharp edges the published/NSSM path doesn't:

- **`ASPNETCORE_URLS` gets silently overridden.** `dotnet run` loads
  [launchSettings.json](../backend/src/HMS.Api/Properties/launchSettings.json)'s launch
  profile (you'll see `Using launch settings from ...` in the output), and the profile's own
  `applicationUrl` wins over an ambient `$env:ASPNETCORE_URLS` you set in the same shell —
  Kestrel binds whatever the profile says, not what the variable says. Pass `--urls` on the
  command line instead, which *does* take precedence — `dotnet run --urls "http://localhost:58158"`
  — and confirm the startup log shows `Now listening on: http://localhost:58158`. Localhost
  is all nginx needs. **Don't use `--urls "http://+:58158"`:** that listens on every
  interface, and with a firewall rule for the port the whole API is then served to the
  internet over plain HTTP (found on staging, 2026-10-13).
- **Port 58158 (or whatever port) must have no inbound firewall rule.** Earlier versions of
  this guide said to add one. Check from the VPS with
  `.\open-deployment-firewall-ports.ps1` (it lists any rule that opens the port) and remove
  it with `-CloseApiPort`; then, from another machine,
  `Test-NetConnection 162.35.105.234 -Port 58158` should report `TcpTestSucceeded: False`.
- **Swagger is on in this mode**, because the launch profile runs the app as `Development`.
  Add `--Swagger:Enabled=false` to the `dotnet run` command line (or set
  `$env:Swagger__Enabled = "false"` first) so the API description isn't published; the nginx
  config also refuses `/swagger/` to anyone but a browser on the VPS itself.
- **`ASPNETCORE_ENVIRONMENT` also comes from the launch profile**, not the ambient shell — so
  `$env:ASPNETCORE_ENVIRONMENT` printing empty doesn't mean the app is running as `Production`;
  check the `Hosting environment:` line the app itself prints at startup instead of trusting
  the shell variable.

To pick up a new commit while running this way: stop the process (`Ctrl+C` in its terminal, or
`Stop-Process -Id <dotnet.exe PID> -Force`), `git pull`/`checkout` the target commit, then
`dotnet run --urls "http://localhost:<port>" --Swagger:Enabled=false` again from `backend/src/HMS.Api` — this recompiles
against the current source and, if `ASPNETCORE_ENVIRONMENT=Development` (the launch profile's
default), auto-runs the Platform + legacy-tenant migration on startup per
[Database Migration Deployment](#database-migration-deployment) above. It will **not** migrate
any real hospital tenant — that still needs the Migrate button, same as the published path.
Treat this mode as an interim/testing state, not a real deployment: migrate to
`install-api-service.ps1` (step 2 above) when convenient, since a `dotnet run` session dies the
moment the terminal/session closes or the VM reboots, with no automatic restart.

## Testing

1. **API from the VPS itself:** `curl http://localhost:58158/health` → `Healthy`.
2. **API port is NOT reachable externally:** from another machine,
   `Test-NetConnection 162.35.105.234 -Port 58158` should report `TcpTestSucceeded: False`
   (unless `-ExposeApiPortDirectly` was used on purpose). If it succeeds, the API is bound
   beyond localhost and a firewall rule opens the port — see step 6.
3. **API via the reverse proxy, public IP:** from another machine,
   `curl http://162.35.105.234/api/v1/...` (any anonymous endpoint) or
   `curl http://162.35.105.234/health`.
4. **Swagger is NOT reachable externally:** open `http://162.35.105.234/swagger/index.html`
   in a browser on another machine and confirm it is refused (403 from nginx; 404 from the
   API itself when it runs as Production or with `Swagger__Enabled=false`). If it loads
   instead, the live nginx config is missing the `allow`/`deny` lines in its `/swagger/`
   block and the API is running in Development mode — check the `Hosting environment:` line
   in the API's startup output rather than assuming the deploy script ran successfully.
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
