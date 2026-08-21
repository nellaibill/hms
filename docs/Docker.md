# Running HMS with Docker

## Purpose
Lets the whole stack (Postgres, API, frontend) run on a laptop with nothing but Docker
Desktop installed — no .NET SDK, no Node, no local Postgres. Intended for demos, not as a
production deployment topology (see [Deployment.md](Deployment.md)).

## What it does NOT change
This reuses the app's existing architecture as-is: EF Core migrations, `TenantMigrationService`
/ `TenantProvisioningService`, `TenantResolutionMiddleware`, the Platform database/tenant-database
split. No second migration mechanism, no schema/behavior changes. The containers run the same
`Program.cs` startup path as `dotnet run` in Development — see "Why Development environment"
below.

## First-time setup

```bash
cp .env.example .env
```

Edit `.env` and set real values for `JWT_SIGNING_KEY`, `SUPER_ADMIN_PASSWORD`,
`PLATFORM_ADMIN_PASSWORD` (any long random string / a password meeting the policy in
[Configuration.md](Configuration.md) — min 10 chars with upper/lower/digit/symbol). These are
the container equivalents of the `dotnet user-secrets` values used in local dev.

```bash
docker compose up -d --build
```

First build downloads the .NET SDK/ASP.NET runtime, Node, Postgres, and nginx base images,
restores ~15 backend projects, and builds the frontend — expect several minutes depending on
network speed. Subsequent builds are fast (Docker layer + BuildKit cache mount reuse).

## URLs

| Service | URL | Notes |
|---|---|---|
| Frontend | http://localhost:8081 | Browser entry point |
| API | http://localhost:8080 | Swagger at `/swagger` |
| Postgres | localhost:5432 | `hms` / (your `POSTGRES_PASSWORD`) — for psql/pgAdmin only |

Change ports via `.env` (`FRONTEND_PORT`, `API_PORT`, `POSTGRES_PORT`) if any of these are
already taken on the host.

## Seeded logins (after first startup)

- **Hospital login** (frontend, hospital code `legacy`, role "Super Admin"): username
  `superadmin`, password = your `SUPER_ADMIN_PASSWORD`. First login forces a password change
  (`mustChangePassword`) — this is existing app behavior, not Docker-specific.
- **Platform Portal** (`/platform`, or `POST /api/platform/auth/login`): email
  `support@yourhms.com`, password = your `PLATFORM_ADMIN_PASSWORD`.

## Why the containers run in the Development environment

`Program.cs` only runs its startup migration/seed block
(`app.Environment.IsDevelopment()`) — this is the *only* place in the app that calls
`Database.Migrate()` today; there's no separate production migration step yet (see
[Deployment.md](Deployment.md)). The compose file sets `ASPNETCORE_ENVIRONMENT=Development`
so a fresh container actually provisions its schema and seed data, then overrides every
secret/connection-string/CORS value `appsettings.Development.json` would otherwise hardcode to
`localhost`, using the standard `Section__Key` environment-variable convention
(docs/Configuration.md) — see the `api` service's `environment:` block in
`docker-compose.yml`.

## Networking

- **API → Postgres**: `Host=postgres` (the Compose service name — Docker's internal DNS),
  not `localhost`.
- **Browser → API**: `http://localhost:8080` (the published host port) — baked into the
  frontend's static bundle at *build* time via `VITE_API_BASE_URL` (Vite inlines
  `import.meta.env.*`; it can't be changed at container-start time). If you change
  `API_PORT` in `.env`, rebuild the frontend image (`docker compose build frontend`).
- No laptop IP address is hardcoded anywhere — everything resolves via Docker service DNS
  (container-to-container) or `localhost` (browser-to-host-published-port), so this works
  identically on any laptop.

## Multi-tenancy in Docker

One Postgres *server* (`postgres` service), many databases — exactly like local dev:
- `hms_platform` — the Platform database (tenant directory, Platform Admin users).
- `hms_qa` (configurable via `TENANT_DB_NAME`) — the seeded "legacy" tenant, matching the
  pre-existing single-dev-tenant convention.
- Every hospital registered afterward (via the Platform Portal or `POST
  /api/platform/hospitals`) gets its own new physical database, created by
  `TenantProvisioningService` exactly as it does outside Docker — nothing about tenant
  creation is Docker-specific.

## Data persistence & backup/restore

Two named Docker volumes persist state across `docker compose down` / `up`:
- `hms_pgdata` — all Postgres data (every tenant + platform database).
- `hms_uploads` — patient/product/document/branding file uploads (`wwwroot/uploads`).

`docker compose up` **does not** touch your existing Windows-native PostgreSQL install —
Docker's Postgres runs as a separate container with its own volume, on whatever host port you
choose (default 5432, only a conflict if your native Postgres also listens on 5432 and both
are running at once — stop one or change `POSTGRES_PORT`).

**Fresh demo (default)**: first `docker compose up` starts from an empty `hms_pgdata` volume
— migrations/seeding create `hms_platform` and the legacy tenant from scratch. This is the
default and recommended path for a reproducible demo.

**Carrying your existing demo data to another laptop** (optional): dump your current
Windows-native Postgres databases and restore them into the Docker Postgres container.

```bash
# On the source laptop — dump each database you want to carry over
pg_dump -h localhost -p 5432 -U hms -Fc hms_platform > hms_platform.dump
pg_dump -h localhost -p 5432 -U hms -Fc hms_qa       > hms_qa.dump
# repeat for any other tenant database you want to carry over

# Copy the .dump files to the destination laptop alongside the repo, then:
docker compose up -d postgres   # start only Postgres first
docker cp hms_platform.dump hms-postgres-1:/tmp/
docker cp hms_qa.dump       hms-postgres-1:/tmp/
docker compose exec postgres psql -U hms -d postgres -c "CREATE DATABASE hms_platform;"
docker compose exec postgres psql -U hms -d postgres -c "CREATE DATABASE hms_qa;"
docker compose exec postgres pg_restore -U hms -d hms_platform /tmp/hms_platform.dump
docker compose exec postgres pg_restore -U hms -d hms_qa       /tmp/hms_qa.dump
docker compose up -d            # start api + frontend against the restored data
```

The API's startup `Database.Migrate()` call is idempotent (skips migrations already applied),
so bringing up `api` against restored databases is safe even if they're on an older schema
version — it just applies whatever's pending, same as it would locally.

## Resetting to a clean demo

```bash
docker compose down -v
docker compose up -d --build
```

`-v` removes both named volumes — next startup is a fresh, empty demo again.

## Known limitations

- **Data Protection keys are ephemeral**: the API logs a warning
  (`Storing keys in a directory ... that may not be persisted`) — MFA-secret encryption keys
  live inside the `api` container's filesystem, not a volume. Recreating the `api` container
  (not just restarting it) invalidates any encrypted Platform Admin MFA secrets. Not an issue
  for a demo; would need a mounted key ring for anything longer-lived.
- **No HTTPS**: the API and frontend both serve plain HTTP, matching this app's current
  no-TLS posture everywhere else (no `UseHttpsRedirection()` in `Program.cs`).
- **`npm ci` doesn't work for the frontend image**: the repo's committed
  `frontend/package-lock.json` was generated on Windows, and npm's optional-dependency
  resolution (a documented npm bug, npm/cli#4828) then fails to install Rollup's Linux native
  binary under `npm ci`/`npm install` when that lock is present — on both Alpine and Debian
  base images. `frontend/web/Dockerfile` works around it by not copying the lockfile into the
  image and letting `npm install` resolve fresh; see the Dockerfile's own comment. The repo's
  committed lockfile is untouched.
