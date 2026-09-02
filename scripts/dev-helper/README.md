# HMS Dev Helper

A local dashboard for running an HMS checkout on a Windows dev/QA VM: Git, database, backend,
and frontend, all from one page with live logs. Windows-only (uses `taskkill` to stop
processes) — see [scripts/setup/windows](../setup/windows) for the VM's other prerequisites.

It only ever runs commands and env vars this repo already documents (see
[docs/DevelopmentGuidelines.md](../../docs/DevelopmentGuidelines.md),
[docker-compose.yml](../../docker-compose.yml), and `HMS.Api/Program.cs`'s `migrate` path) —
it does not invent its own migration/seed logic.

## Setup

```bash
cd scripts/dev-helper
npm install
npm start
```

Open http://localhost:4500. On first run it uses the defaults in `config.example.json`
(base path `C:\hms`) — open **Settings** and either edit fields by hand or point **Base
Path** at your checkout and click **Import defaults from `<basePath>\.env`** to pull in the
Postgres user/password, database names, JWT signing key, and seed passwords already in that
file. Your edits are saved to `config.local.json`, which is gitignored (it holds local
paths and dev passwords, same handling as this repo's own `.env`).

## What each panel does

- **Git** — `git fetch` / `git pull` against the base path.
- **Database** — creates the Platform and Tenant (`ConnectionStrings:Default`) databases if
  missing, then runs `dotnet run --project HMS.Api -- migrate` (the same migrate+seed path
  `docker-compose.yml`'s `migrate` service and `docs/Deployment.md` use). Seeding isn't a
  separate command in this codebase — it happens inside that same step — so "Seed Data" just
  re-runs it; that's cheap because both the migrations and the seeders are idempotent.
- **Backend** — `dotnet run --project HMS.Api`, with the connection strings, JWT signing key,
  and seed passwords from Settings injected as environment variables (the app has no default
  for any of these outside Docker — see `JwtConfiguration.cs` and the seeders' own
  "configuration is incomplete" skip behavior).
- **Frontend** — `npm run dev -- --host 0.0.0.0` in `frontend/web` (after `npm install` at the
  `frontend/` workspace root, so the `@hms/shared` workspace link resolves).
- **Tenant Seed** — registers a new hospital via the real `POST /api/platform/hospitals`
  endpoint, logging in as the seeded Platform Admin first. Needs the backend running and a
  prior "Run All Migrations" (that's what seeds the Platform Admin account).
- **Full Setup** — chains all of the above in order: Git → Build → Create DBs → Migrate/Seed →
  Start Backend → Start Frontend.

## Notes

- The dashboard itself binds to `127.0.0.1` only. The HMS frontend it starts is what binds to
  `0.0.0.0` (per the task spec), so it's reachable from other machines on the VM's network.
- Native (non-Docker) dev in this repo points `ConnectionStrings:Default` and `:Platform` at
  the *same* physical database by convention (see `Program.cs`'s comment on
  `seedLegacyTenant`) — so "Create Platform DB" and "Create Tenant DB" can legitimately be a
  no-op/no-op pair if you leave the Settings defaults alone. Set "Tenant (Default) DB name" to
  something else (e.g. `hms_qa`, matching `docker-compose.yml`'s `TENANT_DB_NAME`) if you want
  two separate physical databases instead.
