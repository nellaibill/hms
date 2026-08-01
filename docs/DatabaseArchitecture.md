# Database Architecture — HMS

This document defines the database architecture and the standards every future database object must follow. It is a design and standards document — it contains no SQL, no business tables, no Entity Framework models, and no relationships between actual hospital entities. It complements [Architecture.md](Architecture.md) (system architecture) and [DatabaseGuidelines.md](DatabaseGuidelines.md) (lightweight index, which points here for depth).

**Stack:** PostgreSQL, .NET 10, Entity Framework Core
**Architecture:** Modular Monolith, single PostgreSQL database, schema-per-module, single-tenant (MVP)

---

## 1. Database Organization

### Why a single PostgreSQL database
The backend is one deployable modular monolith process, not a set of independently deployed services — a single database matches that deployment topology with one connection pool, one backup/restore procedure, and one set of operational runbooks to maintain. For a 3-person team, operating multiple databases multiplies monitoring, credential management, and disaster-recovery work without a corresponding benefit at MVP scale.

A single database also keeps transactional consistency simple: some workflows may need to touch more than one module's data atomically (e.g., cancelling an appointment and reversing an associated invoice line). Within one database this is a normal transaction; across separate databases it becomes a distributed-transaction problem — exactly the kind of complexity this project's constraints (no distributed systems, no event bus) rule out for MVP.

Splitting into separate physical databases remains available later, once real ownership or scaling boundaries emerge — see the migration path below and schema-per-module rationale, which is what makes that split possible without a redesign.

### Why schema-per-module is preferred
PostgreSQL schemas are lightweight, free logical namespaces within one database. Using one schema per backend module:

- Enforces the modular monolith's boundary rule at the data layer — a module's EF Core `DbContext` only owns and migrates its own schema, mirroring the code-level rule that modules never reach into each other's internals (see [Architecture.md](Architecture.md)).
- Makes table ownership self-evident — any object under `patients.*` belongs to the Patients module, which matters for a small team without a dedicated DBA.
- Allows per-schema PostgreSQL permissions to be tightened later (e.g., a read-only reporting role scoped to specific schemas) without redesigning storage.
- Is the natural seam to "cut along" if a module is ever extracted into its own database or service — a schema boundary that was already respected in application code translates directly into a physical boundary later.

### Recommended schema naming conventions
- All lowercase, `snake_case`, no abbreviations unless universally understood.
- Schema name matches its owning backend module 1:1 (e.g., `HMS.Modules.Patients` → `patients`), so the mapping between code module and schema requires no translation table.
- One additional schema, `shared`, holds cross-module reference data with no single owning module (see §2).
- No application tables live in `public`. Reserved/system schemas (`public`, `information_schema`, `pg_catalog`) are left untouched, so every object's ownership is explicit rather than defaulted.

### Future migration path to multi-tenancy
MVP is single-tenant (one hospital/organization). Three paths exist if multi-tenancy becomes a real requirement, in order of increasing isolation and operational cost:

1. **Shared schema, `tenant_id` column** — add a tenant discriminator column (and index) to every business table; enforce isolation via an EF Core global query filter and/or PostgreSQL Row-Level Security. Lowest operational overhead; the natural first step from single-tenant.
2. **Schema-per-tenant** — duplicate the schema set per tenant. Stronger isolation, but multiplies migration and maintenance effort per tenant; justified only when tenants require strict data separation (e.g., regulatory) or very different scale.
3. **Database-per-tenant** — strongest isolation, highest cost; a natural extension when a specific tenant needs physically dedicated infrastructure or data residency.

**Recommendation:** if/when multi-tenancy becomes a real requirement, start with option 1 — it requires the least structural change to the schema-per-module design already in place and is the most reversible. Escalate to options 2 or 3 only for tenants that specifically need it. This is a future decision, not an MVP one; when it becomes concrete, record it in [DecisionLog.md](DecisionLog.md).

---

## 2. Schema Strategy

| Schema | Responsibility | Status |
|---|---|---|
| `identity` | Users, roles, permissions, and authentication-related records. Does not hold clinical or patient demographic data. | MVP |
| `patients` | Patient demographic and registration records. | MVP |
| `appointments` | Scheduling data — slots, bookings, calendars. | MVP |
| `staff` | Doctor/nurse/staff directory and roster data. | MVP |
| `billing` | Invoices, payments, and billing line items. | MVP |
| `notifications` | Notification templates, delivery logs, and reminder scheduling data. | MVP |
| `products` | Product/item master catalog — barcodes, batches, prices, images, dynamic attributes, and tax mappings (docs/04_Product_Management_ERD). FKs into `masters` for classification/unit reference data. | MVP |
| `pharmacy` | Medication inventory and dispensing records. | Reserved — post-MVP module |
| `laboratory` | Lab test orders and results. | Reserved — post-MVP module |
| `inventory` | General hospital supply/equipment inventory (distinct from pharmacy stock). | Reserved — post-MVP module |
| `shared` | Cross-module reference/lookup data with no single owning module (e.g., generic lookups, future outbox or cross-cutting audit infrastructure). | MVP (kept deliberately small) |

`pharmacy`, `laboratory`, and `inventory` are reserved names to prevent future naming collisions and to keep this document a complete map of the domain — they are **not** created until their owning module is actually built. Creating empty schemas ahead of need is avoided; only the current six MVP modules plus `shared` are provisioned now.

---

## 3. Naming Conventions

These are **patterns**, not object definitions — no actual tables, columns, or constraints are created by this document.

| Object | Pattern | Notes |
|---|---|---|
| Table | `{schema}.{entity_plural}` | `snake_case`, plural noun describing the row. No repeated schema prefix inside the name. |
| Column | `{attribute_name}` | `snake_case`, singular. No type suffixes (not `name_varchar`). |
| Boolean column | `is_{state}` / `has_{thing}` | e.g. a flag column reads as a yes/no question. |
| Timestamp column | `{event}_at` | e.g. an audit column recording *when* something happened. |
| Date-only column | `{event}_date` | Distinguishes date-only values from full timestamps. |
| Foreign key column | `{referenced_entity_singular}_id` | Always singular, always suffixed `_id`. |
| Primary key column | `id` | Every table uses the same column name for its own identity. |
| Primary key constraint | `pk_{table}` | |
| Foreign key constraint | `fk_{table}_{referenced_table}[_{column}]` | Column suffix added only when disambiguating multiple FKs to the same referenced table. |
| Index (non-unique) | `ix_{table}_{column(s)}` | |
| Unique index | `ux_{table}_{column(s)}` | |
| Check constraint | `ck_{table}_{rule}` | |
| Default constraint (if named explicitly) | `df_{table}_{column}` | |
| View | `vw_{descriptive_name}` | Lives in the schema of its primary/owning entity, or in `shared` if genuinely cross-schema. |
| Function | `fn_{verb_noun}` | |
| Trigger function | `trg_{table}_{event}` | e.g. describes the table and event it reacts to. |
| Sequence | `seq_{purpose}` | Only for manually managed sequences; identity-column sequences use PostgreSQL's own default naming. |

General rules: all lowercase, `snake_case` throughout, no PostgreSQL reserved words as identifiers, and no abbreviations unless they are unambiguous domain standards (prefer spelling a term out over guessing whether an abbreviation is universally understood).

---

## 4. Primary Key Strategy

| Option | Advantages | Disadvantages |
|---|---|---|
| `BIGINT` (identity/serial) | Smaller (8 bytes), sequential insert locality, human-friendly in logs/debugging, naturally creation-ordered. | Enumerable/predictable (minor exposure risk if used in URLs/APIs), awkward to generate client-side before insert, can collide when merging data across environments (e.g., seed data imports). |
| `UUID` (random v4) | Globally unique without coordination, safe to expose in APIs without leaking sequence/growth information, generatable in the application layer before the row is persisted, trivially mergeable across environments. | Twice the storage (16 bytes), random values fragment the primary key's b-tree, hurting insert locality and cache behavior at large scale, less human-readable. |
| Sequential/time-ordered UUID (UUIDv7-style) | Combines UUID's non-enumerability and coordination-free generation with mostly-sequential insert locality, avoiding random UUID's index fragmentation. | Slightly more time-predictable than random UUID (acceptable, since IDs are never relied on as secrets — see [Authorization.md](Authorization.md)). |

**Recommended approach for this project:** UUID as the primary key type for all business tables, using a time-ordered/sequential generation strategy (UUIDv7 or an equivalent sequential-UUID approach) rather than random v4, generated at the application layer.

Rationale specific to HMS:
- The modular monolith may later split a module into a separately deployed service or database; coordination-free UUIDs avoid primary-key collisions if data ever needs to move between databases.
- Patient-related identifiers benefit from being non-enumerable when referenced in API responses.
- MVP-scale insert volume (a single hospital) is far below the point where random-UUID index fragmentation becomes a real problem, and choosing a sequential UUID scheme from day one avoids ever hitting that problem.
- One PK strategy across every module removes a recurring "which convention does this table use" question for junior developers.

**Exception:** a purely internal, extremely high-churn technical table (e.g., a future outbox or fine-grained audit-log table) may use `BIGINT` identity if a specific, measured performance need arises. Treat this as an explicit, documented exception via [DecisionLog.md](DecisionLog.md), not a default.

---

## 5. Audit Columns

Every business table includes the following, added via a shared EF Core base entity convention (see the backend's `Shared.Kernel` project) rather than repeated by hand per module:

| Column | Purpose |
|---|---|
| `created_at` | When the row was created — baseline auditability, "recently added" sorting, and data-lineage debugging. |
| `created_by` | Who/what created the row — accountability, since "who touched this record" is frequently a compliance requirement in healthcare data. |
| `updated_at` | When the row was last modified — supports "last modified" displays and cache-invalidation logic. |
| `updated_by` | Who last modified the row — same accountability rationale as `created_by`, updated on every write. |
| `is_deleted` | Soft-delete flag — allows a record to be hidden from normal use without physically destroying data that may carry legal/clinical retention requirements. |
| `deleted_at` | When a soft delete occurred — supports retention-policy queries (e.g., "purge records soft-deleted over N years ago") and audit trails. |
| `deleted_by` | Who performed the soft delete — same accountability rationale. |
| `row_version` | Optimistic concurrency token (PostgreSQL's `xmin` system column may serve this purpose directly; otherwise an explicit concurrency-token column) — prevents silent lost-update conflicts when two users edit the same record concurrently, a real risk at a hospital front desk or nursing station. |

---

## 6. Soft Delete Strategy

**Why soft delete is used:** healthcare data frequently carries legal or regulatory retention requirements — medical records, billing history — where physically deleting a row can destroy information that must remain recoverable for audits, legal holds, or simple human error correction. Soft delete preserves the underlying data while letting the application treat the record as gone.

**When hard delete is acceptable:**
- Data with no regulatory or audit significance and no downstream references (e.g., an unsaved draft or genuinely transient row).
- An explicit, user-initiated "permanently delete" action on data nothing else could reference.
- A scheduled, deliberate retention-policy purge of records that have exceeded a defined retention window after being soft-deleted — a logged batch operation, never an ad hoc delete from general application code.

**Query filtering strategy:** `is_deleted = false` is enforced as a global query filter at the EF Core `DbContext` level for each module, so every query excludes soft-deleted rows by default. Viewing soft-deleted records (e.g., an administrative "recover a record" screen) requires explicitly opting out of the filter — this avoids relying on every developer remembering to add the filter by hand.

**Restoration strategy:** restoring a soft-deleted record clears `is_deleted`, `deleted_at`, and `deleted_by` through a dedicated, audited "restore" operation — never through generic CRUD update code — so the restoration itself leaves its own audit trail (who restored the record, and when). Restoration is typically a privileged action (see [Authorization.md](Authorization.md)).

---

## 7. Relationships

- **Foreign Keys:** every relationship is enforced as a real database constraint, not only as an application-level convention — the database is the last line of defense against orphaned data, independent of application bugs.
- **Cascade Delete:** used sparingly, only for true parent-owns-child compositions where the child has no independent lifecycle or meaning outside its parent. Even then, prefer expressing the cascade through the application/domain layer (as a soft-delete cascade) over relying purely on a hard `ON DELETE CASCADE`, since a hard cascade is irreversible and bypasses the soft-delete/audit strategy above.
- **Restrict Delete:** the default for relationships between separate aggregates. Prevents deleting a row that is still referenced elsewhere, forcing an explicit decision (soft-delete, reassign, or block) instead of silently cascading or nulling data a user didn't intend to touch. This should be the default posture for most foreign keys in a hospital system, where accidental data loss has outsized consequences.
- **Optional Relationships:** a nullable foreign key column, paired with "set null on delete" only where "keep the row but lose the reference" is a genuinely valid business state — used deliberately, not by default.
- **Required Relationships:** a non-nullable foreign key column, always paired with restrict-delete (never cascade) unless the child is a true owned composition as described above.
- **Cross-schema references:** PostgreSQL supports a foreign key in one schema referencing a table in another (e.g., an appointment referencing a patient). This is allowed, but treated as a deliberate, reviewed decision rather than a default — it is the physical-database analog of the code-level rule that modules only depend on another module's public `Contracts`, and should be used just as sparingly.

No actual entities or relationships between hospital modules are defined here — this section is policy only.

---

## 8. Indexing Strategy

- **Primary indexes:** created automatically by the primary key constraint. Every table has exactly one; no extra action needed.
- **Foreign key indexes:** PostgreSQL does **not** automatically index the referencing side of a foreign key. Every foreign key column must have an explicit supporting index — without one, both joins against it and the database's own referential-integrity checks on parent update/delete degrade to full scans.
- **Composite indexes:** created for query patterns that filter or sort on more than one column together, with column order matching the most selective/most-commonly-filtered column first. Added based on observed query needs from the Application layer, not spelled out speculatively for every possible column combination.
- **Unique indexes:** used to enforce business uniqueness rules at the database level (the actual guarantee), while application-level checks exist only to provide a friendly error message before the round-trip to the database.
- **Search indexes:** for free-text or partial-match search (e.g., searching a directory by name), prefer PostgreSQL's trigram (`pg_trgm`) or full-text search (`tsvector`/GIN) indexes over naive wildcard scans — introduced only when an actual search feature requires it, not preemptively.
- **General guideline:** every index carries a write-amplification and storage cost. Add indexes to satisfy known query patterns; once the system has real traffic, periodically review index usage and drop indexes that go unused.

---

## 9. Migration Strategy

- **Entity Framework Core migrations:** each module owns its own migration history, generated from that module's own `DbContext` against its own schema only. A change in one module produces a migration touching only that module's schema, keeping migrations scoped and reviewable — consistent with the backend's `HMS.Database.Migrations` project, which aggregates every module's migrations for coordinated deployment (see [Architecture.md](Architecture.md)).
- **Migration naming convention:** EF Core's default `{timestamp}_{DescriptiveName}` pattern, with the descriptive name in PascalCase, imperative, and scoped to one logical change (e.g., naming what was added or changed, not a batch of unrelated changes bundled together).
- **Deployment strategy:** pending migrations are applied as an explicit, logged step in the deployment pipeline before the new application version begins serving traffic (see [Deployment.md](Deployment.md)). At MVP scale, a straightforward "apply pending migrations, then start the app" step is sufficient — no blue/green schema-versioning scheme is needed yet.
- **Rollback strategy:** prefer forward-only migrations (a new migration that undoes a previous change) over EF Core's down-migration tooling for anything already deployed to a shared environment, since rolling back a schema change that already has data written against it is often lossy or impossible. Down-migrations remain useful for local development iteration only.
- **Versioning approach:** the EF Core migrations history table (one per module schema) is the single source of truth for "what version is this database at." No separate manual version-numbering scheme is introduced — the applied migration sequence per schema *is* the version.

---

## 10. Transactions

- **Transaction boundaries:** a transaction wraps exactly one Application-layer use case (one unit of work). It never spans multiple HTTP requests, and it never spans a call into another module — per the modular monolith's in-process communication rule, a use case that needs another module's data treats that data as already committed, rather than enlisting it in the same transaction.
- **Recommended isolation level:** PostgreSQL's default `Read Committed` for the large majority of operations — the right balance of correctness and concurrency for typical CRUD-style hospital workflows. Escalate to `Repeatable Read` only for specific operations that read-then-write based on a value that must not change mid-transaction (e.g., checking and booking a limited scheduling slot in the same operation), and treat that escalation as a deliberate, documented exception rather than a default.
- **When to use transactions:** any operation writing to more than one table where partial completion would leave data inconsistent (e.g., booking an appointment and adjusting slot availability together). Single-table, single-row writes generally don't need an explicit application-level transaction beyond the atomicity EF Core's own save operation already provides.
- **Long-running transaction considerations:** never hold a transaction open across an external call (an HTTP call to a notification provider, waiting on user input, etc.). Long-held transactions hold locks and prevent PostgreSQL's autovacuum from reclaiming dead tuples, degrading performance system-wide. External calls happen before or after a transaction, never inside one.

---

## 11. Performance Guidelines

- **Query optimization:** project queries to only the columns actually needed (EF Core projections to DTOs) rather than always loading full entities; watch for N+1 query patterns (missing eager/explicit loading) during code review, since this is the most common EF Core performance mistake for teams newer to the ORM.
- **Pagination:** every list-returning endpoint paginates — no endpoint returns an unbounded result set. Offset-based pagination is acceptable and simpler at MVP data volumes; keyset (seek) pagination is the recommended upgrade path once a specific table's size or query pattern makes offset pagination noticeably slow.
- **Bulk operations:** operations touching many rows use set-based EF Core APIs (bulk update/delete) or set-based SQL rather than loading entities into memory one at a time and saving them in a loop, which does not scale and generates excessive round-trips.
- **Connection management:** rely on Npgsql's built-in connection pooling (enabled by default) rather than building custom pooling. Keep the configured pool size aligned with the database server's connection limit and the number of running application instances (documented in [Configuration.md](Configuration.md)); never hold a connection open longer than a single unit of work.
- **Partitioning (future consideration):** not needed at MVP scale (single hospital, single tenant). Flagged as a future option for high-volume, time-series-like tables (e.g., an audit log or a results-over-time table) once row counts justify it — PostgreSQL's native range partitioning (typically by date) is the natural mechanism. Explicitly deferred, consistent with this project's constraint against premature infrastructure complexity.

---

## 12. Security Guidelines

- **Least privilege:** the running application connects to PostgreSQL using a role that can only read/write application schemas — no superuser and no schema-creation privileges. Migrations run under a separate, more-privileged role used only during deployment, distinct from the runtime application role, so a compromised running application cannot alter schema.
- **Database roles:** at minimum, separate roles for (a) migrations/DDL, used only by the deployment pipeline, (b) the application runtime, limited to data reads/writes with no DDL, and optionally (c) a read-only role reserved for future reporting/BI needs. Per-module database roles are not necessary at MVP scale (schema-level grants within one application role are sufficient), but the schema-per-module layout makes splitting roles per module straightforward later if it becomes necessary.
- **Secrets management:** database credentials are never stored in source control or committed configuration files (see [Configuration.md](Configuration.md) and [Security.md](Security.md)). Local development uses .NET User Secrets; deployed environments use environment variables or the hosting platform's secret store. Credentials are rotated periodically and immediately upon any suspected compromise.
- **Encryption considerations:** connections to PostgreSQL require TLS in every environment, including production; data at rest relies on the hosting/managed-PostgreSQL provider's disk encryption. Column-level encryption is reserved for a small, explicitly identified set of highly sensitive fields only if a specific compliance requirement demands it beyond transport and disk encryption — it is not applied broadly by default, to keep querying and indexing straightforward for MVP.
- **Backup and recovery recommendations:** automated daily full backups plus continuous write-ahead-log archiving (point-in-time recovery), ideally provided by the managed PostgreSQL hosting platform rather than hand-rolled scripts. The restore procedure is documented and periodically *tested*, not just configured — an untested backup is not a reliable backup. Backup retention follows the same regulatory/legal retention considerations noted in the soft-delete strategy (§6).

---

## 13. Deliverables

This document is the Database Architecture Design deliverable for the HMS project, maintained at `docs/DatabaseArchitecture.md`. It should be read alongside:

- [Architecture.md](Architecture.md) — overall system architecture and module boundaries
- [DatabaseGuidelines.md](DatabaseGuidelines.md) — lightweight index/template pointing here for depth
- [NamingConventions.md](NamingConventions.md) — general naming rules across the whole codebase
- [Security.md](Security.md), [Configuration.md](Configuration.md) — secrets and environment handling
- [DecisionLog.md](DecisionLog.md) — where future deviations from this document (e.g., the multi-tenancy path, or a BIGINT PK exception) should be recorded when they actually occur

No SQL, business tables, Entity Framework models, or relationships between hospital modules were generated — this is architecture, conventions, and standards only.
