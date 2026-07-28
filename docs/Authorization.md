# Authorization

## Purpose
This document defines the role/permission model and how access control is enforced across the system, so it is clear who can do what and where that rule is checked.

## Scope
Covers roles, permissions, and enforcement points across frontend and backend.

**Out of scope:** how users prove identity (see [Authentication.md](Authentication.md)) and general security practices (see [Security.md](Security.md)).

## When to Update This Document
- A role or permission is added, removed, or changed.
- A new module introduces new access rules.
- An edge case in access control is discovered and resolved.

## Recommended Sections
- Overview
- Roles Defined
- Permission Model
- Enforcement Points (API / Frontend)
- Role Assignment Process
- Edge Cases
- API Endpoints (Roles & Permissions)

---

## Overview

Access control follows plain RBAC: a **Role** is a named bundle of **Permissions**, and a user is granted access by being assigned a Role. There is no per-user permission override — all access flows through the role.

**Current implementation status (2026-07-28):**
- The Roles Management UI (`frontend/web/src/features/roles/`) is fully built — list page, create/edit/view form, and a module × action permission matrix.
- It currently runs entirely on **mock data in `localStorage`** (`mockRolesStore.ts`) — there is no real API wired up yet.
- The backend `Roles` module directory exists but has no source code and is not referenced in the solution. No `permissions` / `role_permissions` tables or migrations exist yet.
- The `Identity` module's `User` entity has no `RoleId` field yet, so role-to-user assignment is not implemented anywhere.

This document specifies the target model (normalized RBAC) and the API contract the backend needs to implement so the existing frontend can be pointed at a real service with no reshaping of its data model.

## Roles Defined

Roles are user-defined via the UI (not a fixed enum) — an admin can create, rename, or deactivate roles. The following are the current **demo/seed** roles used for local development and design reference (`frontend/web/src/features/roles/mockRoles.ts`); they are not a fixed list the backend needs to hardcode:

| Role | Description | Status |
|---|---|---|
| Super Admin | Unrestricted access to every module, including system configuration. | Active |
| Doctor | Clinical consultation, prescriptions, and diagnostic order access. | Active |
| Nurse | Ward charting, patient vitals, and medication administration. | Active |
| Receptionist | Patient registration, appointment scheduling, and front-desk billing. | Active |
| Pharmacist | Prescription fulfillment, drug master, and stock/batch tracking. | Active |
| Lab Technician | Sample tracking, test order queue, and result entry. | Active |
| Accountant | Invoice ledger, payments, insurance claims, and financial reporting. | Active |
| HR Manager | Staff directory, roster/shift assignment, and leave management. | Inactive |
| Front Desk (Trainee) | Limited, view-only access used while shadowing during onboarding. | Inactive |

A role has: `name` (unique), `description`, `status` (`Active` \| `Inactive`), and a set of granted permissions.

## Permission Model

Permissions are **not free-form strings** — they are a fixed matrix of **10 modules × 4 actions = 40 permission flags** per role. Modules and actions are backend-seeded, not user-editable through the UI.

**Modules** (`module` id — label):
- `patient-management` — Patient Management
- `clinical-care` — Clinical Care
- `diagnostics` — Diagnostics & Ancillary
- `pharmacy` — Pharmacy
- `support-services` — Support Services
- `finance-billing` — Finance & Billing
- `records-compliance` — Records & Compliance
- `workforce-admin` — Workforce & Administration
- `engagement` — Engagement
- `reports-analytics` — Reports & Analytics

**Actions:** `view`, `create`, `edit`, `delete`

Each `(module, action)` pair is one `Permission` row (40 total). A `Role` grants a subset of these via the `role_permissions` join table. **These module ids must match `frontend/web/src/features/roles/modules.ts` exactly** — if they drift, the permission matrix UI silently breaks (the UI renders fixed rows keyed by these ids).

## Enforcement Points

**Frontend (current):**
- `features/roles/components/PermissionMatrix.tsx` renders the 10×4 toggle grid; `RoleForm.tsx` composes it with role info fields.
- Route-level gating today uses a *separate, unrelated* mechanism: a hardcoded `Role` enum (`superAdmin | admin | receptionist | doctor | nurse | ...`) in `frontend/web/src/features/auth/types.ts`, consumed by `config/navigation.ts` to filter the nav menu. This is not yet connected to the module/permission matrix described above.

**Backend (target, not yet implemented):**
- Every API endpoint that mutates or reads protected data must check the caller's effective permissions (derived from their assigned role) before executing — e.g. `patients.create` requires `patient-management.create`.
- The `/me` (or equivalent current-user) endpoint should return the caller's effective permissions flattened from their role, so the frontend can gate buttons/menus without extra round trips.

**Open item:** the frontend's auth-enum-based nav gating and the module/permission-matrix model need to be reconciled — either the nav enum is retired in favor of effective permissions from the real role, or the two are explicitly kept separate (route gating vs. UI action gating).

## Role Assignment Process

**Not yet implemented.** To assign a role to a user, the `Identity` module's `User` entity needs a `RoleId` foreign key (currently absent — see `backend/src/Modules/Identity/HMS.Modules.Identity/Domain/User.cs`). Once added, assignment would happen either:
- as part of the existing user create/update payload (`roleId` field), or
- via a dedicated `PUT /api/v1/users/:id/role` endpoint.

This needs to be scoped with whoever owns the Users/Identity module — it's a dependency of this feature, not something the Roles module can deliver alone.

## Edge Cases

- **Deleting a role currently assigned to users:** should be blocked (`409` with the affected user count) rather than silently cascading — see `DELETE /api/v1/roles/:id` below. Not yet exposed in the UI (no Delete action exists on the Roles list today, despite full CRUD being expected on the backend — confirm with backend dev whether this was intentionally deferred).
- **`status: Inactive` role:** needs a product decision — does it block login/access immediately for users holding it, or only hide it from the "assign role" picker for new assignments?
- **Module/action catalog drift:** the 40 permission keys are duplicated in two places (frontend `modules.ts` and backend seed data) until a `GET /api/v1/permissions` catalog endpoint is added and the frontend is updated to consume it dynamically. Until then, adding a module requires coordinated changes on both sides.
- **Super Admin role:** currently modeled as "all 40 flags granted" (40/40), not a special bypass flag — enforcement logic should not assume a hardcoded "super admin" shortcut.

## API Endpoints (Roles & Permissions)

Target contract for the backend team, shaped to match the existing frontend's data model exactly (`frontend/web/src/features/roles/types.ts`) so no reshaping is needed once wired up.

### `GET /api/v1/permissions`
Backend-seeded catalog of the 40 fixed `(module, action)` pairs. Not called by the UI on every page load today (rows are hardcoded client-side), but needed as the join target for `role_permissions`.

```json
[
  { "id": "perm-001", "module": "patient-management", "action": "view", "key": "patient-management.view", "label": "View" }
]
```

### `GET /api/v1/roles`
Powers the Roles list page. Query params: `search` (role name), `status` (`Active` | `Inactive`), `sort` (`name` | `updatedAt`), `page`, `pageSize`.

```json
{
  "items": [
    {
      "id": "role-002",
      "name": "Doctor",
      "description": "Clinical consultation, prescriptions, and diagnostic order access.",
      "status": "Active",
      "permissionCount": 16,
      "totalPermissionCount": 40,
      "updatedAt": "2026-07-01T15:40:00.000Z"
    }
  ],
  "total": 9,
  "page": 1,
  "pageSize": 20
}
```

### `GET /api/v1/roles/:id`
Powers both the View page and the Edit page (prefill).

```json
{
  "id": "role-002",
  "name": "Doctor",
  "description": "Clinical consultation, prescriptions, and diagnostic order access.",
  "status": "Active",
  "createdAt": "2026-01-12T09:00:00.000Z",
  "updatedAt": "2026-07-01T15:40:00.000Z",
  "permissions": {
    "patient-management": { "view": true,  "create": false, "edit": true,  "delete": false },
    "clinical-care":      { "view": true,  "create": true,  "edit": true,  "delete": true  },
    "diagnostics":        { "view": true,  "create": true,  "edit": true,  "delete": false },
    "pharmacy":           { "view": true,  "create": false, "edit": false, "delete": false },
    "support-services":   { "view": false, "create": false, "edit": false, "delete": false },
    "finance-billing":    { "view": false, "create": false, "edit": false, "delete": false },
    "records-compliance": { "view": true,  "create": false, "edit": false, "delete": false },
    "workforce-admin":    { "view": false, "create": false, "edit": false, "delete": false },
    "engagement":         { "view": false, "create": false, "edit": false, "delete": false },
    "reports-analytics":  { "view": false, "create": false, "edit": false, "delete": false }
  }
}
```

### `POST /api/v1/roles`
Save on the "New Role" form.

Request: `{ "name", "description", "status", "permissions": { ...full 10-module matrix } }`
Response `201`: same shape as `GET /roles/:id`.
Errors: missing `name` → `400`; duplicate `name` → `409`.

### `PUT /api/v1/roles/:id`
Save on the "Edit Role" form. Request/response shapes identical to `POST`. Full replace of the permission matrix on every save (matches the "grid + one Save button" UI) — no partial-patch semantics needed.

### `DELETE /api/v1/roles/:id`
Not wired up in the UI yet (no Delete action exists on the Roles list today). Confirm with backend dev whether this is built but unlinked, or deferred.
Response `204` on success, or `409 { "usersAssignedCount": n }` if the role is currently assigned to any user.
