# Offline-First Patient Sync (HMS.Modules.Sync)

**Status:** Implemented on branch `feature/offline-first-sync` (ADR-133). Not deployed anywhere yet.
**Default:** `Sync:Role = None`. Merging this changes nothing for an existing deployment: no schema is created, nothing is captured, no job runs.

---

## 1. What it does

Each hospital can run HMS on a **local (Edge) server**. Patient work is saved there first and never waits for the internet. Every patient change also writes an **outbox event in the same database transaction**. A **Hangfire** job pushes those events, in order per patient, to the **cloud HMS** over an authenticated API. The cloud applies each event exactly once, in the same transaction as its acknowledgement.

Both roles run the same build. `Sync:Role` decides what a server does: `None` (default), `Edge` or `Cloud`.

```
 HOSPITAL SITE (Sync:Role = Edge)                                CLOUD (Sync:Role = Cloud)
 ┌───────────────────────────────────────────────┐              ┌───────────────────────────────────────────┐
 │ React: existing pages + /admin/cloud-sync     │              │ HMS.Api                                   │
 │        │ JWT (unchanged)                      │              │  POST /api/v1/sync/patients               │
 │        ▼                                      │              │  GET  /api/v1/sync/ping                   │
 │ HMS.Api                                       │              │   auth: "HmsSync <key>" → hospital        │
 │  PatientService (unchanged)                   │              │   per event, ONE transaction:             │
 │   └ PatientsDbContext.SaveChanges             │              │    inbox_events (EventId idempotency)     │
 │       └ PatientOutboxInterceptor ──┐ same tx  │              │    replica_streams (sequence + stamp)     │
 │  ┌──────────── hospital DB ────────▼───────┐  │              │    conflicts (both versions, no LWW)      │
 │  │ patients.*     sync.outbox_events       │  │              │    IPatientSyncService → patients.*       │
 │  │                sync.outbox_streams      │  │  HTTPS       │  sync-cutover → cloud patient writes 409  │
 │  │                sync.outbox_attempts     │  │  ordered     └───────────────────▲───────────────────────┘
 │  │                sync.sync_state          │  │  batches                         │
 │  └──────────────────────▲──────────────────┘  │                                  │
 │                         │ claim: FOR UPDATE SKIP LOCKED + lease                    │
 │  Hangfire (hms_platform, schema "hangfire"), retries OFF                           │
 │   sync-dispatch (every N min) → SyncTenantJob(tenantId) ───────────────────────────┘
 │   sync-probe    (every min)   → GET ping: reachable / key rejected
 │   sync-cleanup  (daily)       → purge synced events older than RetentionDays
 │  /hangfire (one-time link → 15-min HttpOnly cookie)                                │
 └────────────────────────────────────────────────────────────────────────────────────┘
```

The browser never talks to the cloud, the outbox tables or Hangfire storage directly. Cloud keys exist only in the Edge server's environment.

---

## 2. Decisions (approved with the proposal)

1. **The local server owns patient records** once a hospital is cut over. The cloud then refuses patient registration and edits for that hospital (`409 PATIENTS.MANAGED_BY_LOCAL_SERVER`), so UHIDs cannot collide and conflicts stay rare. Reads keep working.
2. **Scope: the Patient aggregate** (patient, address, allergies, emergency contacts). The engine is generic (`aggregate_type`, stream = aggregate id), so visits or billing can be added later.
3. **Hangfire dashboard:** users with `identity-administration.edit`, Edge server only, through a one-time link from the Cloud Sync page.

### Where the implementation differs from the proposal, and why

| Proposal | Implemented | Why |
|---|---|---|
| `"sync"` as a FeatureCatalog SchemaBacked + Mandatory key, migrated for every tenant | Not a feature key. The `sync` schema is migrated only when `Sync:Role` is `Edge` or `Cloud`; `sync_registry` only on `Cloud` | On this development machine `hms_platform` is owned by `postgres` and the app role has no CREATE right there. An unconditional migration made a plain `dotnet run` fail. Gating on the role keeps `None` deployments exactly as they were, and keeps the Platform Admin's feature screen unchanged |
| `platform.tenant_sync_clients` in the Platform module | `sync_registry.sync_clients` and `sync_registry.tenant_settings`, owned by the Sync module, in `hms_platform` | Leaves the Platform module untouched. A separate schema name avoids a clash with the per-hospital `sync` schema when the legacy hospital shares `hms_platform` |
| Tenant flag `PatientsOwnedByEdge` set at cutover | `dotnet HMS.Api.dll sync-cutover <code> on\|off` writes `sync_registry.tenant_settings` | Same effect, no Platform change, auditable by the operator who runs it |
| Conflict check on the patient row's `xmin` | A **version stamp**: SHA-256 over `(table, id, xmin)` of the patient, address, allergies and contacts | A cloud-side edit to an allergy or contact changes no patient-row `xmin` and would have gone undetected |
| — | **Outbox epoch** per hospital database, sent with each batch and stored per patient on the cloud | If a local database is ever re-created, its numbering restarts at 1; without an epoch the cloud would treat the new events as duplicates and drop them |
| Inbox stores every result | Inbox stores only **Applied** and **Skipped** events | A Rejected or Conflict result must be re-evaluated after an admin fixes data; a stored rejection would be replayed forever |
| `POST /dashboard-session` sets the cookie directly | It returns a **one-time link** (60 s, single use); opening it sets the cookie and redirects | Works through nginx and across origins in development, without cross-origin cookie handling in the React client |
| Hangfire.PostgreSql 1.20.x | 1.21.1, plus a `Newtonsoft.Json` 13.0.3 pin | Current release. Hangfire.Core otherwise resolves Newtonsoft.Json 11.0.1, which has a high-severity advisory (GHSA-5crp-9r3c-p9vr) and would trip the ADR-078 gate |
| — | Optional `ConnectionStrings:Hangfire` | Lets Hangfire live outside `hms_platform` where the app role cannot create schemas there |

---

## 3. Data model

All tables are created by EF migrations in `HMS.Database.Migrations/Sync` and `/SyncRegistry`.

### Edge side (`sync` schema, every hospital database)

**`sync.outbox_events`**: one row per committed patient change.

| Column | Notes |
|---|---|
| `id` (PK), `event_id` (unique) | UUIDv7. `event_id` is the cloud idempotency key |
| `stream_id`, `aggregate_type`, `aggregate_id` | Stream = patient id |
| `operation_type` | `Insert`, `Update`, `Delete`, `Snapshot` (backfill) |
| `sequence_number` | 1, 2, 3 … per patient; unique `(stream_id, sequence_number)` |
| `payload` (jsonb), `payload_schema_version`, `payload_hash` | Full post-change snapshot. Hash = SHA-256 of PostgreSQL's canonical jsonb text, recomputed by the cloud |
| `changed_fields` (text[]) | Property **names** only, never values |
| `status`, `retry_count`, `next_retry_at`, `last_attempt_at`, `synced_at` | `Pending`, `Processing`, `Synced`, `Failed`, `NeedsAttention`, `Conflict` |
| `error_code`, `error_message` | Never contains patient values |
| `cloud_receipt_id`, `cloud_processed_at` | Cloud acknowledgement |
| `resolution`, `resolution_note`, `resolved_by`, `resolved_at` | Admin resolution (`LocalWins`, `CloudWins`, `Skip`) |
| `conflict_snapshot` (jsonb) | Cloud copy returned with a conflict, for "keep cloud version" |

Indexes: `ux_outbox_events_event_id`, `ux_outbox_events_stream_id_sequence_number`, `ix_outbox_events_status_next_retry_at`, partial `ix_outbox_events_unsynced (stream_id, sequence_number) WHERE status <> 'Synced'`, partial `ix_outbox_events_synced_at WHERE status = 'Synced'`.

**`sync.outbox_streams`**: one row per patient. The sequence allocator (`INSERT … ON CONFLICT DO UPDATE SET last_sequence = last_sequence + 1 RETURNING`), the claim target, and the monitoring row. Columns: `last_sequence`, `last_synced_sequence`, `status`, `retry_count`, `next_attempt_at`, `last_attempt_at`, `last_error_code`, `lease_owner`, `lease_expires_at`. Partial index `ix_outbox_streams_claimable WHERE last_synced_sequence < last_sequence`.

**`sync.outbox_attempts`**: every send attempt (attempt no, worker, outcome, HTTP status, error, duration).

**`sync.sync_state`**: one row (`id = 1`): `outbox_epoch`, `is_paused`, `auth_failed`, `cloud_reachable`, probe details, `last_successful_sync_at`, `last_run_at`.

### Cloud side (`sync` schema, every hospital database)

- **`sync.inbox_events`**: `event_id` PK, stream, sequence, payload hash, `Applied`/`Skipped`, receipt id, source node, processed at.
- **`sync.replica_streams`**: per patient: `last_applied_sequence`, `last_applied_event_id`, `applied_version_stamp`, `source_epoch`.
- **`sync.conflicts`**: both snapshots, `Open` / `ResolvedLocalWins` / `ResolvedCloudWins`.

### Cloud registry (`sync_registry` schema, `hms_platform`)

- **`sync_registry.sync_clients`**: `tenant_id`, `hospital_code`, `key_prefix`, `key_hash` (SHA-256, unique), `created_at/by`, `revoked_at`, `last_used_at`.
- **`sync_registry.tenant_settings`**: `patients_owned_by_edge` per hospital.

### Hangfire (`hangfire` schema, `hms_platform` or `ConnectionStrings:Hangfire`)

Created by Hangfire.PostgreSql on first Edge start. Job arguments hold a tenant id at most: no patient data, no secrets.

---

## 4. Write path (Edge)

`PatientOutboxInterceptor` (EF `SaveChangesInterceptor`, Patients module) is added to `PatientsDbContext` only when an `IOutboxWriter` is registered, which happens only on an Edge server. Every patient write path ends in one `SaveChangesAsync` (registration, edit, delete, allergy and contact add/remove, bulk import), so `PatientService` is unchanged.

1. **Saving:** read the change tracker for Patient / Address / Allergy / EmergencyContact; group by patient; work out `Insert` / `Update` / `Delete` and the changed property names. Start a transaction if the caller has none; otherwise join the caller's.
2. EF writes the patient rows.
3. **Saved:** per patient (in id order, so row locks are always taken in one order), re-read the whole aggregate, then one statement allocates the next sequence number and inserts the outbox row on the **same connection and transaction**. Commit only a transaction the interceptor started.
4. **Failed / cancelled:** roll back. A failure to write the event fails the save itself, so a patient change can never commit without its event.

`IOutboxWriter` lives in `HMS.Shared.Kernel.Sync` (plain `System.Data.Common` types); the Patients module never references the Sync module (enforced by architecture tests).

---

## 5. Sync engine (Edge)

### Jobs

| Job | Schedule | What it does |
|---|---|---|
| `sync-dispatch` | every `IntervalInMinutes` | Queues one `SyncTenantJob` per active hospital with a configured key |
| `SyncTenantJob(tenantId)` | queued | Claim → send one ordered batch → write results back, in a loop until nothing is claimable, a transport failure, or `MaxRunSeconds` |
| `sync-probe` | every minute | `GET /api/v1/sync/ping` per hospital; records reachable / unreachable / key rejected. Sending is gated on it |
| `sync-cleanup` | daily 02:30 UTC | Deletes synced events (and attempts) older than `RetentionDays`. Unsynced events are never deleted |

Every sync job has `[AutomaticRetry(Attempts = 0)]`, and the same filter is applied globally. **The outbox row is the only retry record**, so a Hangfire retry can never send something twice.

### Claiming and ordering

```sql
UPDATE sync.outbox_streams s
   SET lease_owner = @worker, lease_expires_at = now() + make_interval(secs => @lease_seconds), last_attempt_at = now()
 WHERE s.stream_id IN (
       SELECT c.stream_id FROM sync.outbox_streams c
        WHERE c.last_synced_sequence < c.last_sequence
          AND c.status NOT IN ('NeedsAttention', 'Conflict')
          AND (c.next_attempt_at IS NULL OR c.next_attempt_at <= now())
          AND (c.lease_expires_at IS NULL OR c.lease_expires_at < now())
        ORDER BY c.next_attempt_at NULLS FIRST, c.stream_id
        LIMIT @limit
        FOR UPDATE SKIP LOCKED)
RETURNING s.stream_id, s.last_synced_sequence;
```

- Two workers or two servers never claim the same patient and never wait for each other.
- The row lock lasts one statement. The **lease** holds the claim during the HTTP call, so no transaction is held across it. A crashed worker's lease expires and the stream is picked up again; the cloud's EventId check makes the re-send harmless.
- Each claimed patient contributes its next events (up to `MaxEventsPerStream`) in sequence order. Whole patients are added until the batch would exceed `BatchSize`. A stream whose events do not continue from its last synced number is parked as `NeedsAttention` (`SYNC.SEQUENCE_GAP`), never sent out of order.
- Results are written back in one short transaction, only while the worker still holds the lease.
- **Within a patient**, events succeed in order up to the first non-success. That event fails; every later event of that patient stays `Pending`. **Other patients are unaffected.**

### Failure handling

| What happened | Event / stream | Retries used |
|---|---|---|
| `Applied`, `Duplicate`, `Skipped` | `Synced`, receipt stored | — |
| No answer: DNS, refused, socket, timeout | Back to `Pending`; stream backs off `ConnectivityBackoffSeconds`; probe marks the cloud unreachable and sending waits for it | **None** |
| HTTP 401 / 403 | Back to `Pending`; `sync_state.auth_failed`; sending waits until the probe authenticates | **None** |
| Other HTTP error (5xx, 408, 429, …) or unreadable answer | Each patient's first event `Failed`; the rest `Pending` | One, on that first event |
| Per-event `Transient` / `OutOfOrder` | `Failed`, retry after `RetryScheduleMinutes` (1, 2, 5, 10, 30, then 30) ±10% jitter | One |
| More than `MaxRetryCount` retries | `NeedsAttention` (`SYNC.RETRIES_EXHAUSTED`) | — |
| Per-event `Rejected` (bad payload, unknown state/district, UHID clash, unknown payload version, hash mismatch) | `NeedsAttention`, no automatic retry | — |
| Per-event `Conflict` | `Conflict`, cloud copy stored | — |
| This server shutting down mid-call | Back to `Pending`, leases released | None |

All state is in the database, so a restart loses nothing.

---

## 6. Cloud receiver

`POST /api/v1/sync/patients` and `GET /api/v1/sync/ping` accept **only** the `HmsSync` scheme (`Authorization: HmsSync <key>`). The key is checked by SHA-256 hash against `sync_registry.sync_clients` (not revoked) and the hospital must be active. `TenantResolutionMiddleware` resolves the hospital from the key before the controller is built. The scheme authorizes nothing else, and a hospital user token cannot call these endpoints.

Per event, **one transaction**:

1. EventId already in `inbox_events` → `Duplicate` with the original receipt. Nothing is written.
2. Lock the patient's `replica_streams` row. Sequence ≤ last applied → `Duplicate`; > last + 1 → `OutOfOrder`. A different `source_epoch` restarts numbering at 1.
3. Recompute the payload hash; a mismatch is `Rejected`.
4. Resolution markers: `Skip` / `CloudWins` record the event as `Skipped` and move the sequence on (`CloudWins` adopts the current cloud copy as the new baseline).
5. **Conflict check:** if the cloud copy's version stamp differs from the one recorded after the last sync apply, record both versions in `sync.conflicts` and return `Conflict`. Nothing is overwritten. `LocalWins` skips this check. A patient seen for the first time has no baseline and is adopted.
6. Apply through `IPatientSyncService.ApplySnapshotAsync` on the same connection and transaction (Patients module: upsert patient, address; add, update and remove allergies and contacts by id; validates state/district).
7. Insert the inbox row, update `replica_streams` (sequence, new stamp, epoch), commit. Data change and acknowledgement commit together.

Within one patient, the cloud stops at the first non-success and returns the rest as `NotAttempted`.

**Write guard (Decision 1).** After `sync-cutover <code> on`, `CloudPatientWriteGuardFilter` answers `409 PATIENTS.MANAGED_BY_LOCAL_SERVER` to any non-GET request to the Patients and Patient Import controllers for that hospital.

---

## 7. Conflict resolution (no last-write-wins)

- **Detection:** strict per-patient sequence numbers stop a stale event from overwriting newer data, and the version stamp catches any cloud-side edit outside sync.
- **On conflict:** the cloud stores both versions; the local event becomes `Conflict` and blocks only that patient. Logs carry ids only.
- **Resolution** on `/admin/cloud-sync` (needs `identity-administration.edit`, written to the Activity Log):
  - **Send this server's version** (`LocalWins`): the event is re-sent and applied without the conflict check. The cloud's earlier copy stays in `sync.conflicts`.
  - **Keep the cloud's version** (`CloudWins`): the cloud copy is applied to the local record through the normal captured save, so a new event carrying it queues behind. The blocked event is sent as a marker that moves the cloud's sequence on and adopts its copy as the baseline. Both sides end identical.
  - **Skip** (`NeedsAttention` only): the event is sent as a skip marker. Every event carries the full record, so the patient's next change brings the cloud up to date.

---

## 8. Monitoring

### API: `api/v1/sync-monitor` (any role; reports "not enabled" unless Edge)

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/summary` | `identity-administration.view` | Overall status, cloud link, pause/auth state, last sync, counts |
| GET | `/streams?search=&status=&page=&pageSize=` | view | Patients, blocked first. `status` may be `Unsynced` |
| GET | `/streams/{patientId}/events` | view | History with attempts (field names only) |
| GET | `/events?status=&page=&pageSize=` | view | Events by status; default Failed, NeedsAttention, Conflict |
| POST | `/events/{eventId}/retry` | `identity-administration.edit` | Retry the event blocking its patient |
| POST | `/events/retry-all` | edit | Retry every Failed / NeedsAttention event (not conflicts) |
| POST | `/events/{eventId}/resolve` | edit | `{ "resolution": "LocalWins" \| "CloudWins" \| "Skip", "note": "…" }` |
| POST | `/pause`, `/resume` | edit | Stop/start sending. Capture continues |
| POST | `/trigger` | edit | Probe, then run a sync now |
| POST | `/dashboard-session` | edit | One-time link to the Hangfire dashboard |

Patient name and UHID appear only for callers who also hold `patient-management.view`. Nothing returns payloads, field values, the cloud address or keys. Every action is written to the Activity Log (module `Sync`, ids only).

### React page: `/admin/cloud-sync`

Sidebar → Administrative → **Cloud Sync** (next to Activity Log); route-gated on `identity-administration.view`. Status banner, server ↔ cloud link, this computer's network (`navigator.onLine`), local database, last successful sync, counts; Patients and Failed changes tabs; history drawer; retry, retry all, resolve, skip, pause, resume, sync now and job dashboard (actions only with `.edit`). Refreshes every 15 s.

### Hangfire dashboard: `/hangfire` (Edge only)

"Job dashboard" asks the API for a one-time link (60 s, single use, DataProtection-encrypted). Opening it sets an **HttpOnly, SameSite=Strict** cookie scoped to `/hangfire` (Secure over HTTPS), valid 15 minutes, and redirects. Without a valid cookie every dashboard URL answers 401. `DisplayStorageConnectionString` is off.

---

## 9. Configuration and secrets

```jsonc
"Sync": {
  "Role": "None",               // None | Edge | Cloud
  "Enabled": true,              // Edge: sending on/off. Capture continues when off
  "IntervalInMinutes": 1,       // 1–59
  "BatchSize": 100,             // events per HTTP batch
  "MaxEventsPerStream": 20,     // per patient per batch
  "MaxRunSeconds": 50,          // one run drains until this
  "MaxRetryCount": 5,
  "RetryScheduleMinutes": [1, 2, 5, 10, 30],
  "HttpTimeoutInSeconds": 30,
  "LeaseSeconds": 120,          // must be ≥ HttpTimeoutInSeconds + 30
  "ConnectivityBackoffSeconds": 60,
  "RetentionDays": 30,
  "NodeId": "",                 // defaults to the machine name
  "Cloud": { "BaseUrl": "" }    // Edge: https://… (http only for localhost); empty = capture-only
},
"Hangfire": { "WorkerCount": 2, "SchemaName": "hangfire", "DashboardPath": "/hangfire" }
```

- **The cloud key is a secret.** Set it only as an environment variable on the Edge server: `Sync__Cloud__ApiKeys__<HOSPITALCODE>=hmssync_…` (NSSM `AppEnvironmentExtra`, or user-secrets in development). Hospital codes match case-insensitively.
- Options are validated at start-up (`ValidateOnStart`): a misconfigured Edge refuses to start instead of silently not syncing.
- **Capture-only Edge.** With `Sync:Role=Edge` and no `Sync:Cloud:BaseUrl` (or no key for a hospital), every patient change is still saved with its outbox event, and nothing is sent. The Cloud Sync page shows **Not configured** with the queue growing. Add the address and key later and restart: the backlog is sent in order.
- **Edge start-up.** Before accepting requests, an Edge server creates or updates the `sync` tables of every active hospital (a hospital whose database cannot be reached is logged and skipped, never created).

**Local development (Edge, capture-only).** On a machine where the app's role cannot create schemas in `hms_platform`, give Hangfire its own database:

```bash
dotnet user-secrets set "Sync:Role" "Edge" --project backend/src/HMS.Api
dotnet user-secrets set "ConnectionStrings:Hangfire" "Host=localhost;Port=5432;Database=hms_sync_hangfire;Username=hms;Password=hms" --project backend/src/HMS.Api
# later, when a cloud is available:
dotnet user-secrets set "Sync:Cloud:BaseUrl" "https://<cloud>" --project backend/src/HMS.Api
dotnet user-secrets set "Sync:Cloud:ApiKeys:<hospitalCode>" "<key>" --project backend/src/HMS.Api
# switch sync off again:
dotnet user-secrets remove "Sync:Role" --project backend/src/HMS.Api
```
- `ConnectionStrings:Hangfire` (optional) moves Hangfire's tables out of `hms_platform`.
- `appsettings.json` lists `RetryScheduleMinutes` as an array. An environment override replaces elements by index (`Sync__RetryScheduleMinutes__0`, `__1`, …), so override all of them together.

---

## 10. Installation and rollout

Deploy the **cloud first** whenever the payload format changes.

1. **Merge** with `Sync:Role = None` everywhere. Nothing changes.
2. **Cloud server**
   1. Set `Sync__Role=Cloud`, deploy, run `dotnet HMS.Api.dll migrate` (creates `sync` in every hospital database and `sync_registry` in `hms_platform`; needs CREATE rights there).
   2. Issue the hospital's key: `dotnet HMS.Api.dll sync-key issue LHS`. It is printed **once**; only its hash is stored.
3. **Local (Edge) server**
   1. Install HMS with the existing Windows scripts.
   2. Restore the hospital's latest **cloud backup** onto it so both sides start identical (same patient and Masters ids).
   3. Set `Sync__Role=Edge`, `Sync__Cloud__BaseUrl=https://<cloud>`, `Sync__Cloud__ApiKeys__LHS=<key>`. Run `migrate`, then start the service. Hangfire creates its schema on first start (CREATE rights on `hms_platform`, or set `ConnectionStrings__Hangfire`).
   4. Add the nginx `location /hangfire` block (in `scripts/deploy/windows/nginx-hms-reverse-proxy.conf`).
   5. Patients that existed before capture was on: `dotnet HMS.Api.dll sync-backfill LHS` queues one full snapshot per patient that has never been synced. Safe to repeat.
4. **Cutover:** on the cloud, `dotnet HMS.Api.dll sync-cutover LHS on`. From then on the cloud refuses patient writes for that hospital. Front desk switches to the local URL.
5. **Operations:** `sync-key list LHS`, `sync-key revoke LHS [prefix]` (then issue a new key and update the Edge environment variable), `sync-cutover LHS off`.
6. **Rollback:** set `Sync__Role=None` on the Edge and restart. Capture and sending stop; the outbox stays for a later resume. On the cloud, `sync-cutover LHS off` re-opens cloud patient editing.

**Do not** restore an Edge database from an older backup of itself while it has unsynced events: its sequence numbers go back and the cloud treats the replayed numbers as already applied. After such a restore, run `sync-backfill` on a fresh database instead, or contact the developers.

---

## 11. Edge cases

| # | Scenario | Behaviour | Verified by |
|---|---|---|---|
| 1 | Internet drops mid-sync | Connectivity: events back to Pending, no retry used, probe pauses sending | Integration `Offline_…`; live run (cloud stopped) |
| 2 | Reconnects after hours | Probe reopens sending; runs drain the backlog | Live run: drained ~90 s after the cloud returned |
| 3 | Local app restarts with pending events | All state in the database | Live run: event queued before an Edge restart was delivered after it |
| 4 | Job dies mid-processing | Lease expires; re-send deduplicated by EventId | Integration `EventsLeftProcessingByACrash_…`, `TwoWorkers_…` |
| 5 | Same event sent twice | Inbox returns the original receipt | Integration `ResendingAnAcknowledgedEvent_…`; HTTP `ValidKey_…` |
| 6 | One patient fails, others pending | Only that patient is blocked | Integration `FiveUpdates_…` (bystander), unit planner tests |
| 7 | Five updates, #3 fails | #1–2 Synced, #3 blocked, #4–5 Pending | Integration `FiveUpdates_ThirdRejected_…`; unit planner test |
| 8 | Cloud HTTP 500 | One retry on each patient's first event, 1-minute back-off | Integration `ServerError_…`; unit planner |
| 9 | Cloud HTTP 401 | Key rejected; no retries used; resumes when the probe authenticates | Integration `RejectedKey_…`; live run (revoke, re-issue, restart) |
| 10 | Cloud slower than the timeout | Treated as connectivity; re-send is safe | Integration `Timeout_…` |
| 11 | Two workers, same patient | `FOR UPDATE SKIP LOCKED` + lease | Integration `TwoWorkers_…` |
| 12 | Cloud database down | Per-event `Transient` (or HTTP 5xx) → back-off | Unit planner; receiver catches transient Npgsql errors |
| 13 | User edits a patient during sync | Short row lock; new event queues behind | Integration `ConcurrentEditsOfOnePatient_…` (gap-free sequences) |
| 14 | Shutdown during sync | Outcome `Cancelled`, leases released | Unit planner (`Cancelled_…`) |
| 15 | Permanently invalid event | `NeedsAttention`, patient blocked, admin retry/skip, audited | Integration `FiveUpdates_…`; live run (skip via the page) |

---

## 12. Tests and validation (run on 2026-11-11)

| Suite | Result |
|---|---|
| `dotnet build` (HMS.Api and tests) | 0 errors |
| Unit tests (`HMS.UnitTests`) | **1,953 passed**, 0 failed (all suites, including 65 new sync/capture tests) |
| Architecture tests (`HMS.ArchitectureTests`) | **131 passed**, 0 failed (includes new Sync boundary rules) |
| Integration tests, Sync only (`HMS.IntegrationTests`, filter `Modules.Sync`) | **23 passed**, 0 failed, against throwaway databases on local PostgreSQL 18 via `HMS_TEST_POSTGRES` |
| `npm run build` (shared + web, includes `tsc --noEmit`) | Passed |
| ESLint on the new/changed frontend files | Clean |

Run the integration tests:

```bash
# With Docker running (Testcontainers starts PostgreSQL):
dotnet test backend/tests/HMS.IntegrationTests --filter "FullyQualifiedName~Modules.Sync"

# Or against an existing PostgreSQL 13+ server (needs CREATEDB; the HTTP tests also need pgvector):
HMS_TEST_POSTGRES="Host=localhost;Port=5432;Username=hms;Password=<pw>;Database=postgres" dotnet test backend/tests/HMS.IntegrationTests --filter "FullyQualifiedName~Modules.Sync"
```

**Live two-server run.** A real Cloud API and a real Edge API ran on four throwaway databases with Hangfire on its normal schedule:

- 7 events from registration, edits, an allergy and a delete synced within one minute. A hash over every row of the four patient tables matched on both sides.
- Cloud stopped: edits saved locally, events stayed Pending with 0 retries, the probe marked the cloud unreachable. On restart the backlog drained and the data matched.
- A cloud-side edit became a Conflict and was not overwritten; it was resolved from the page ("Send this server's version").
- A district missing on the cloud became NeedsAttention; it was skipped from the page, and the next change converged.
- Key revoked: the page showed "Key rejected" with nothing sent. A new key plus an Edge restart recovered, and the queued event was delivered.
- The dashboard link opened the Hangfire dashboard. Anonymous `/hangfire` and forged links answered 401.
- No errors in either server's log; no patient values in the logs.

---

## 13. Known limitations

- **Patients only.** Visits, billing, masters and documents are not synced yet; the cloud shows a cut-over hospital's patients but not its local visits or bills.
- **One direction.** Changes flow local → cloud. The only cloud → local path is the "keep cloud version" resolution.
- **Masters must match.** A state/district created only on the local server makes that patient `NeedsAttention` (`UNKNOWN_DISTRICT`) until the master exists on the cloud.
- **CREATE rights.** `migrate` on a Cloud server and Hangfire's first start on an Edge server need CREATE on `hms_platform` (or `ConnectionStrings:Hangfire` pointing elsewhere). On this development machine `hms_platform` is owned by `postgres`.
- **Rate limit.** Sync requests count against the per-IP anonymous limit (200/min by default). At 100 events per batch that is far above a hospital's write rate.
- **Self-restore.** Restoring an Edge database from an older backup of itself regresses sequence numbers (section 10).
- **One dashboard per server.** The Hangfire dashboard shows every hospital's jobs on that Edge server (tenant ids only).
- **Not covered here:** Docker was not running, so the Testcontainers path was not exercised in this session (the same tests ran against local PostgreSQL). The pre-existing integration suites (Users, HR, …) were not run. Nothing has been deployed to staging. The new page was checked at desktop width only. Pre-existing NU1902/NU1903 package advisories on main (SixLabors.ImageSharp, System.Security.Cryptography.Pkcs/Xml) are unrelated and untouched.

---

## 14. Files

**New module** `backend/src/Modules/Sync/HMS.Modules.Sync/`: `SyncModule.cs`, `SyncCommandLine.cs`, `Contracts/` (role, statuses, wire and monitor DTOs, auth defaults), `Application/` (options + validator, retry policy and result planner, tenant processor, receiver, monitor service), `Domain/SyncRecords.cs`, `Infrastructure/` (DbContexts, configurations, outbox writer, outbox store, cloud client, API keys, auth handler, cloud write guard, dashboard access, Hangfire jobs), `Endpoints/` (receiver and monitor controllers).

**Migrations:** `HMS.Database.Migrations/Sync/` (`InitialCreateSync`), `/SyncRegistry/` (`InitialCreateSyncRegistry`) and their design-time factories.

**Shared kernel:** `HMS.Shared.Kernel/Sync/IOutboxWriter.cs`.

**Patients module:** `Contracts/PatientSyncContracts.cs`, `Application/IPatientSyncService.cs`, `Domain/PatientReplicaState.cs`, replica methods on `Patient`, `Allergy`, `EmergencyContact`, `Infrastructure/Sync/` (interceptor + change collector, snapshot mapper, sync service), registration in `PatientsModule.cs`.

**Host:** `ModuleRegistration.cs`, `Program.cs` (dashboard, CLI, registry migrate on Cloud), `TenantMigrationService.cs` (sync schema when role ≠ None), `TenantResolutionMiddleware.cs` (HmsSync branch), `SecurityHeadersConfiguration.cs` (dashboard CSP exclusion), `appsettings.json` (defaults).

**Frontend:** `frontend/shared/dtos/sync/sync.ts`, `api-client/services/syncMonitorApi.ts`, `constants/routes.ts`; `frontend/web/src/pages/cloudSync/CloudSyncPage.tsx`, `features/cloudSync/` (utils, hooks, status cards, tables, dialogs), `routes/routes.tsx`, `config/navigation.ts`, `services/apiClient.ts`.

**Tests:** `HMS.UnitTests/Modules/Sync/*`, `HMS.UnitTests/Modules/Patients/Infrastructure/PatientSyncCaptureTests.cs`, `HMS.ArchitectureTests/Modules/Sync/SyncModuleBoundaryTests.cs`, `HMS.IntegrationTests/Modules/Sync/*`.

**Deployment:** `scripts/deploy/windows/nginx-hms-reverse-proxy.conf` (`location /hangfire`); `Directory.Packages.props` (Hangfire packages, Newtonsoft.Json pin).
