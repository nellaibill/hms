# Backend Implementation Plan — Roles & Permissions

High-level implementation roadmap for adding Roles & Permissions to the backend. Companion to [Authorization.md](Authorization.md) (API contract) and [DatabaseSchema.md](DatabaseSchema.md) (tables/columns/constraints). This plan is scoped to mirror the already-working `Patients` module's structure and conventions rather than introducing a new pattern.

---

## Decision to confirm before starting

**Roles & Permissions should be implemented inside the existing `HMS.Modules.Identity` module — not the currently empty, orphaned `HMS.Modules.Roles` folder** (which today contains only stray `bin`/`obj` build artifacts and no source, and isn't referenced in `HMS.sln`).

**Why:**
- [DatabaseArchitecture.md](DatabaseArchitecture.md) §2 already scopes the `identity` schema to "users, roles, and permissions" — there is no separate `roles` schema defined anywhere in that document.
- If Roles were its own module, `users.role_id` would be a foreign key reaching across module/schema boundaries. The architecture doc treats cross-schema references as a deliberate exception requiring sign-off, not a default — folding Roles into Identity avoids the issue entirely.
- `HMS.Modules.Identity` already has a working `DbContext`, DI registration, controller pattern, and test coverage to extend, which is materially less work than standing up a new module from scratch.

**Action:** delete the empty `HMS.Modules.Roles/bin` and `/obj` leftovers as part of this work. If the team has a specific reason to keep Roles as a standalone module, revisit this plan before Phase 1 — it changes where every file below lives.

The rest of this plan assumes Roles & Permissions live inside `HMS.Modules.Identity`.

---

## Phase 1 — Domain layer

`backend/src/Modules/Identity/HMS.Modules.Identity/Domain/`

| File | Purpose |
|---|---|
| `Role.cs` | New aggregate root (extends the shared `Entity` base). Factory method `Role.Create(name, description, status)`; behavior methods `Rename`, `SetPermissions`, `Activate`/`Deactivate`. |
| `Permission.cs` | New reference entity: `module`, `action`, `key`, `label`. No behavior — effectively a lookup row. |
| `User.cs` (existing) | Add `RoleId` and an `AssignRole(Guid roleId)` method. |

`role_permissions` does not need its own domain class — model it as a child collection on `Role` (e.g. `Role.PermissionIds`), the same way `Patient` owns `PatientRegistration` as a child of its aggregate.

## Phase 2 — Contracts (public surface)

`.../Contracts/`: `CreateRoleRequest.cs`, `UpdateRoleRequest.cs`, `RoleResponse.cs`, `RoleListQuery.cs`, `PermissionResponse.cs` — records, shaped to match [Authorization.md](Authorization.md)'s request/response bodies exactly, following the same style as `CreatePatientRequest.cs` / `PatientResponse.cs`.

## Phase 3 — Application layer

`.../Application/`

- `IRoleService.cs` (public) + `RoleService.cs` (internal) — `CreateAsync`, `UpdateAsync`, `GetByIdAsync`, `ListAsync`, `DeleteAsync`. Returns `Result<T>` / `PagedResult<T>`, not exceptions, matching `PatientService`.
- `Validators/CreateRoleRequestValidator.cs`, `UpdateRoleRequestValidator.cs` — FluentValidation, registered explicitly in DI (not assembly-scanned, per this codebase's existing convention for validators).
- `RoleErrorCodes.cs` — e.g. `NotFound`, `DuplicateName`, `InUse` (backs the delete-blocked-by-assigned-users case from `Authorization.md`'s Edge Cases).
- `Mapping/RoleMappingExtensions.cs` — `Role` ↔ `RoleResponse`, including the flat `role_permissions` rows ↔ the nested module/action matrix the frontend expects.

## Phase 4 — Infrastructure

`.../Infrastructure/`

- Extend `IdentityDbContext` with `DbSet<Role>`, `DbSet<Permission>`, and the `role_permissions` join.
- `Configurations/RoleConfiguration.cs`, `PermissionConfiguration.cs` — table/column/constraint mapping exactly per [DatabaseSchema.md](DatabaseSchema.md) (names, cascade/restrict rules, indexes).
- Extend `UserConfiguration.cs` with the new `role_id` FK and `ix_users_role_id`.
- `Repositories/IRoleRepository.cs` / `RoleRepository.cs` — mirrors `IPatientRepository`.
- Permission catalog seed data (40 rows) via migration `HasData`, not a runtime seeding step, since the catalog is static.

## Phase 5 — Endpoints

`.../Endpoints/RolesController.cs`

- `[Route("api/v1/roles")]`, structured like `PatientsController`: inject `IRoleService` and validators, map `Result` failures to HTTP status, wrap success in `ApiResponse<T>`.
- One action per endpoint defined in `Authorization.md`: `GET /roles`, `GET /roles/:id`, `POST /roles`, `PUT /roles/:id`, `DELETE /roles/:id`.
- A small additional action (or thin controller) for `GET /api/v1/permissions` — read-only, no service-layer ceremony needed since it's a static catalog read.

## Phase 6 — Migration

`backend/src/Database/HMS.Database.Migrations/Identity/Migrations/`

- One new EF Core migration (reusing the existing `IdentityDbContextFactory`) adding `roles`, `permissions`, `role_permissions`, and `users.role_id`.
- `role_id` lands **nullable** first — existing users have no role yet. Backfilling and tightening to `NOT NULL` is a deliberate follow-up migration, not part of this one.
- Seed the 40 permission rows as migration `HasData`.

## Phase 7 — DI registration

- Extend `IdentityModule.AddIdentityModule(...)` with the new repository, service, and validators.
- No change needed to `HMS.Api/Configuration/ModuleRegistration.cs` — Identity is already wired into the composition root.
- Delete the empty `HMS.Modules.Roles/bin` and `/obj` leftovers (see decision above).

## Phase 8 — Tests

Following this codebase's existing "every module carries an equivalent test" convention:

- `HMS.UnitTests/Modules/Identity/Domain/RoleTests.cs`, `Application/RoleServiceTests.cs` — NSubstitute-mocked repository, mirrors `UserServiceTests.cs`.
- `HMS.IntegrationTests/Modules/Identity/RolesApiTests.cs` — Testcontainers PostgreSQL, mirrors `UsersApiTests.cs` and its `UsersApiFactory`.
- Extend the existing `IdentityModuleBoundaryTests` / `CrossModuleDependencyTests` to cover the new `Role`/`Permission` types (enforcing "internal outside `Contracts`").

## Phase 9 — Frontend cutover

Not part of the backend developer's task, but the definition of "done" for the feature as a whole:

- Swap `frontend/web/src/features/roles/mockRolesStore.ts` calls for a real `rolesApi.ts` hitting the endpoints above.
- Add a `roles` entry to `API_ROUTES` in `frontend/shared/constants/routes.ts`.
- Wire up the missing Delete action in the Roles list UI once `DELETE /api/v1/roles/:id` exists.
