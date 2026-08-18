# Decision Log

## Purpose
This document is an Architecture Decision Record (ADR) style log capturing *why* significant technical decisions were made, so the reasoning isn't lost or locked in one person's head — important given the team has a single senior developer.

## Scope
Covers significant, hard-to-reverse technical or architectural decisions (e.g., choosing Modular Monolith over Microservices, choosing PostgreSQL, module boundary decisions).

**Out of scope:** day-to-day implementation choices that don't affect the broader system.

## When to Update This Document
- Whenever a significant technical or architectural decision is made.
- Whenever a previously recorded decision is revisited or reversed (add a new entry; do not delete the old one).

## Recommended Sections
- Decision Entry Template (Context, Decision, Status, Consequences)
- Chronological list of decisions (newest first)

---

## Decision Entry Template

### ADR-000: [Decision Title]
**Date:** YYYY-MM-DD
**Status:** Proposed / Accepted / Superseded / Deprecated

**Context**
_To be documented._

**Decision**
_To be documented._

**Consequences**
_To be documented._

---

## Decisions

### ADR-011: Roles Management's permission catalog (`ROLE_MODULES`) was missing `identity-administration` — fixed by adding it, not by fetching `GET /api/v1/permissions` dynamically
**Date:** 2026-08-18
**Status:** Accepted

**Context**
A review of the Billing/Roles "mock-data" concern found Roles Management further along than assumed: `apiRoleRepository.ts` already calls the real `RolesController` end-to-end (list/get/create/update, with `activate`/`deactivate` follow-up calls to work around create/update not carrying `status`), falling back to `mockRolesStore.ts` only on a genuine `NetworkError` — the same resilience pattern used by Patients and Masters, not a "fake by default" implementation. `RolesListPage.tsx` already surfaces a "Demo data — API not connected" badge whenever that fallback is active.

The one real bug: `frontend/web/src/features/roles/modules.ts`'s hand-maintained `ROLE_MODULES` list (used to render the permission matrix, build a new role's empty permission grid, and reconcile the backend's flat `permissionKeys` into that grid) had 10 entries, but the real seeded catalog (`PermissionSeedData.cs`) has 11 — `identity-administration`, the module gating Roles/Users/Masters/Settings themselves, was missing. Consequence was worse than a display gap: `apiRoleRepository.ts`'s `toPermissionKeys()` only emits keys for modules in `ROLE_MODULES`, so saving an edit to any role that already held `identity-administration.*` permissions (Super Admin, if ever edited through this UI) would have silently stripped them on save — the matrix never showed them as granted, so nothing would prompt an editor to notice they'd be lost.

**Decision**
Added `{ id: 'identity-administration', label: 'Roles, Users & Settings' }` to `ROLE_MODULES` and corrected the file's doc comment (it claimed "Roles Management has no backend yet," which was already false before this change). Considered fetching the catalog live from `GET /api/v1/permissions` (`PermissionsController` already exists and is read-only/system-seeded) instead of hand-mirroring it, which would remove this whole class of drift permanently — but that requires threading a resolved module list through `apiRoleRepository.ts`'s four exported functions and both Roles pages' loading states, a real refactor across ~8-10 files. Given the catalog is explicitly documented as static and system-seeded (not something an admin edits), the same low-drift-risk tradeoff already accepted for `config/navigation.ts`'s hand-mirrored permission-group strings applies here — a one-line fix now, with the dynamic-fetch version left as a follow-up if the catalog starts changing often enough for manual sync to become a real problem.

**Consequences**
The permission matrix, new-role defaults, and DTO reconciliation all pick up the fix for free — every call site in this module already iterates `ROLE_MODULES` rather than a separately-maintained list, so no other file needed to change. Verified against the live backend: role permission counts changed from "X / 40" to the correct "X / 44" everywhere (confirmed against Super Admin, who holds all 44), and a role created with `identity-administration.view`/`.edit` in its `permissionKeys` round-tripped through the real `RolesController` correctly. Anyone adding a 12th permission module to `PermissionSeedData.cs` in the future must remember to mirror it here too, same as they already must for `config/navigation.ts` — this is a known manual-sync point, not a solved one.

### ADR-009: Documents module ships as a real backend without full platform-wide RBAC, without existence validation for most owner types, and without a real virus scanner — each gap is enforced-narrow and logged, not silently absent
**Date:** 2026-08-06
**Status:** Accepted

**Context**
The Documents module (see [docs/modules/Documents/DocumentManagement.md](modules/Documents/DocumentManagement.md)) aggregates the most sensitive artifact of every other module in one place — patient consent forms, vendor contracts, billing invoices — behind one generic API. Building it to the same "no authentication yet, actorId: null" standard as every other module's controller (see ADR pattern in PatientsController) would have meant shipping the highest-value attack surface in the system on the weakest foundation. At the same time, standing up full ASP.NET Core policy-based authorization (docs/ApiStandards.md §9), existence-checking all ten owner types, and integrating a real antivirus engine are each platform-wide or external-dependency undertakings disproportionate to one module's first iteration.

**Decision**
Three narrow, explicitly-scoped compromises, each with a stated boundary and a clear seam to close it later:
1. `DocumentsController` is the first controller to require `[Authorize]` on every action and derive a real actor from JWT claims. `DocumentAccessPolicy` enforces a real, in-code role-to-owner-type (plus classification) access table today, rather than the dynamic database-backed policy model described in docs/ApiStandards.md §9 — closing that gap fully is platform-wide work affecting every module's controllers, not something Documents should do unilaterally.
2. Owner-existence validation (US-1) is implemented for Patient only, since it's the only owner type with both a real backend module and code this change could verify end-to-end. The other nine either have no backend at all or were left unwired rather than guessed at. An unregistered owner type is accepted with a logged warning, not silently treated as validated.
3. The virus-scan pipeline (US-9) — queue, background worker, Pending/Available/Quarantined state machine — is real, but `IVirusScanner`'s registered implementation (`NullVirusScanner`) always reports clean. No ClamAV or equivalent is available in this environment. This is stated in code comments, in the module doc's Risks section, and here, rather than left to be discovered later.

**Consequences**
Documents can be exercised and reviewed end-to-end today without waiting on platform-wide RBAC or an external AV dependency. Each of the three gaps has a single, well-defined swap-in point (`DocumentAccessPolicy`, `IDocumentOwnerExistenceChecker` per owner type, `IVirusScanner`) rather than being scattered through the module — closing any one of them does not require touching `DocumentService` or `DocumentsController`. Anyone deploying this module against real patient data must treat all three as open items, not completed work, until closed.

### ADR-008: Patient Edit form is scoped to demographic/contact/medical fields only — encounter and billing are not editable there
**Date:** 2026-08-01
**Status:** Accepted

**Context**
`PatientEditForm.tsx` referenced "the MVP-scope ADR" in a code comment without one actually existing in this log — a dangling reference. The underlying scope decision itself is real and was reaffirmed while reviewing the registration wizard: `PatientRegistrationForm.tsx` collects four things a returning edit shouldn't casually touch — Registration Details (encounter type, department, consultant, admission type) and Billing (charges, discounts, payment status) both have consequences beyond the patient record itself (an encounter is tied to a specific visit; a bill is tied to a specific `Billing`/`BillingItem` record created once at registration — see `frontend/web/src/features/billing/`). Folding either into a general-purpose "edit patient" form invites silent, hard-to-audit changes to data that other flows depend on.

**Decision**
`PatientEditForm` stays limited to Patient Information, Contact Information, and Medical Information — the same three tabs it has today. Registration Details and Billing are not exposed there, now or as part of any near-term follow-up; changes to either belong in their own dedicated flows (e.g., a future "amend registration" or "adjust billing" action scoped and audited on its own terms), not bolted onto patient demographic edits.

**Consequences**
`PatientEditForm` intentionally has no draft-persistence or Billing step, unlike `PatientRegistrationForm` — that asymmetry is by design, not a gap to close. Anyone tempted to add a fifth tab to the edit form should treat that as a new decision requiring its own review, not a natural extension of this one.

### ADR-007: Frontend HTTP client must not swallow `AbortError`, and queries use `networkMode: 'always'`
**Date:** 2026-07-23
**Status:** Accepted

**Context**
While smoke-testing the Users web feature against a live dev server, a query against a deliberately-unavailable backend got stuck showing a loading state indefinitely instead of settling into an error. Investigation traced it to two compounding issues in `frontend/shared/api-client/httpClient.ts` and the default React Query configuration: (1) the HTTP client wrapped every `fetch` rejection — including a deliberate `AbortError` from query cancellation — into a generic `NetworkError`, which defeats React Query's ability to distinguish "cancelled on purpose" from "really failed"; (2) the default `networkMode: 'online'` can pause retries based on perceived connectivity rather than surfacing a prompt error.

**Decision**
`HttpClient.request()` now rethrows `AbortError` unchanged instead of wrapping it. `queryClient`'s default `networkMode` is set to `'always'` for both queries and mutations, so a failed request always settles into a catchable error rather than an indefinite paused/loading state.

**Consequences**
Network failures now reliably reach the UI's error-handling path (docs/FrontendArchitecture.md §8) instead of occasionally hanging. Both web and mobile `queryClient.ts` carry this configuration; any future module reusing the shared HTTP client gets the fix for free.

### ADR-006: Users module layers are `internal`, with `InternalsVisibleTo` granted only to the unit test project
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The approved backend architecture states that only a module's `Contracts` namespace is public; `Domain`/`Application`/`Infrastructure` should be `internal` so other modules and the host can't accidentally reach into them. The first implementation pass left everything `public` for expediency.

**Decision**
`User`, `IUserService`/`UserService`, `IUserRepository`/`UserRepository`, `UserErrorCodes`, `UserMappingExtensions`, and both validators are `internal`. `IdentityDbContext` and `UserConfiguration` remain `public` (Database.Migrations legitimately needs them — the one sanctioned cross-project exception the architecture already documents). `HMS.Modules.Identity` grants `InternalsVisibleTo("HMS.UnitTests")` so unit tests can exercise internals directly; no other assembly gets this grant. `HMS.ArchitectureTests` enforces this boundary going forward via NetArchTest rules, since reflection can inspect internal types without needing the grant.

**Consequences**
Every future module should follow the same pattern: internal by default, `Contracts` public, `InternalsVisibleTo` scoped only to that module's own unit test project, and an architecture test asserting it.

### ADR-005: EF Core migration for `identity.users` is hand-authored; `Designer.cs`/`ModelSnapshot.cs` are intentionally omitted
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The .NET SDK is not installed in the environment this module was built in, so `dotnet ef migrations add` could not be run. The migration's `Up()`/`Down()` methods are ordinary, reviewable C# and were hand-written to match `UserConfiguration` exactly. The `.Designer.cs` and `ModelSnapshot.cs` files, however, are tool-generated, reflection-driven, and must be byte-for-byte consistent with the live model — a hand-written approximation risks being subtly wrong in a way that silently corrupts the *next* migration's diff.

**Decision**
Ship the hand-authored `Up()`/`Down()` migration as a reviewable reference, but do not fabricate the Designer/Snapshot files. A README next to the migration documents the exact `dotnet ef migrations add` command to run (and diff against) once the SDK is available.

**Consequences**
The migration cannot be verified to actually apply until someone runs it against a real PostgreSQL instance with the real tooling. This is a known gap, not a silent one — see the Phase 10 quality checklist in the Users module delivery notes.

### ADR-004: Controller-based endpoints (not Minimal API) for module HTTP surfaces
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The backend architecture document left the `Endpoints/` layer's implementation mechanism open ("Minimal API or Controllers"). Building the first reference module forced a concrete choice.

**Decision**
Use ASP.NET Core MVC controllers (`[ApiController]`, `ControllerBase`). Each module's controller lives in its own `Endpoints/` folder but requires a `FrameworkReference` to `Microsoft.AspNetCore.App` since modules are plain class libraries, not Web SDK projects.

**Consequences**
This is now the precedent every future module follows for consistency. Controllers were chosen over Minimal API for familiarity (attribute routing, model binding, filters are well-understood by a team with mixed experience levels) — not because Minimal API is unsuitable; this could be revisited if a future module's needs push toward Minimal API instead, but the choice should stay uniform across modules rather than mixed.

### ADR-003: Manual DTO↔entity mapping instead of AutoMapper/Mapster
**Date:** 2026-07-23
**Status:** Accepted

**Context**
docs/Architecture.md §7 recommended Mapster (or AutoMapper as an alternative) for Application↔Contracts mapping. With a single entity in the first module, either library is pure overhead.

**Decision**
Use a small static extension method (`UserMappingExtensions.ToResponse()`) instead of adding a mapping library dependency.

**Consequences**
Revisit once a module has enough DTOs/entities that hand-written mapping becomes repetitive boilerplate rather than a one-line method — likely by the third or fourth module. Until then, every module should follow this same manual-mapping pattern rather than introducing a library independently.

### ADR-002: Central Package Management (`Directory.Packages.props`) adopted for the whole backend
**Date:** 2026-07-22
**Status:** Accepted

**Context**
Building the first module required adding several new NuGet packages (EF Core, Npgsql, FluentValidation, testing libraries) across multiple projects.

**Decision**
Every package version is declared once in `Directory.Packages.props`; individual `.csproj` files reference packages by name only.

**Consequences**
Adding a package to a new module means adding/reusing a `<PackageVersion>` entry at the root and a bare `<PackageReference Include="..." />` in the project — no version number is ever repeated. Keeps every module on the same package versions by construction.

### ADR-001: The `User` entity carries no credential/authentication fields
**Date:** 2026-07-22
**Status:** Accepted

**Context**
The Users module was explicitly scoped to exclude authentication, JWT, login, and refresh tokens for this iteration (see docs/modules/Identity/Users.md).

**Decision**
`identity.users` (and the `User` domain entity) has no password hash, credential, or session-related column. Only profile data (name, email, phone) and the standard audit columns exist.

**Consequences**
When the Authentication iteration begins, credential fields are added to this same table as purely additive columns — no redesign of the existing schema, entity, or API contracts is expected. If that assumption turns out to be wrong (e.g., credentials need a separate table for security-isolation reasons), record that as a new ADR when it happens.
