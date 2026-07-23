# Users Module (Identity)

This document is the business and functional reference for the Users module — the first reference implementation for HMS. Every future module (Patients, Staff, Appointments, Billing, Pharmacy, Laboratory, Inventory) should follow the same documentation shape.

## Module Purpose

The Users module manages the directory of people who can access the HMS system — front-desk staff, clinicians, and administrators. It owns the **identity record** for a person (name, contact details, active/inactive status) inside the `identity` PostgreSQL schema.

**Explicitly out of scope for this iteration** (per the current constraints — see [DecisionLog.md](../../DecisionLog.md)):
- Authentication (login, password/credential storage)
- JWT issuance or refresh tokens
- Roles and permissions / authorization policies

Those are designed in [Authentication.md](../../Authentication.md) and [Authorization.md](../../Authorization.md) and will be layered on top of this module's user records in a future iteration, without requiring changes to the shape of the core `User` record established here.

## Functional Requirements

1. An authorized operator can create a new user record with a first name, last name, email, and optional phone number.
2. An authorized operator can update an existing user's profile fields.
3. An authorized operator can soft-delete a user record.
4. An authorized operator can retrieve a single user by ID.
5. An authorized operator can retrieve a paginated list of users, with sorting.
6. An authorized operator can search users by name or email.
7. An authorized operator can filter the user list by active/inactive status.
8. An authorized operator can activate a previously deactivated user.
9. An authorized operator can deactivate an active user.

## Non-Functional Requirements

- Every list endpoint is paginated — no endpoint returns an unbounded result set (see [ApiStandards.md](../../ApiStandards.md) §6).
- Email uniqueness is enforced at the database level (unique index), not only in application code.
- All API responses follow the standard envelope and error shape defined in [ApiStandards.md](../../ApiStandards.md).
- All timestamps are stored in UTC and exchanged as ISO-8601 (see [ApiStandards.md](../../ApiStandards.md) §11).
- Soft delete is used, per [DatabaseArchitecture.md](../../DatabaseArchitecture.md) §6 — a deleted user's data is retained, not destroyed.
- This module establishes the reference folder/layer structure, naming conventions, and response shape that every future module must reproduce.

## User Stories

- As a front-desk administrator, I want to create a new user record so that a new staff member can be represented in the system.
- As a front-desk administrator, I want to edit a user's contact details so the directory stays accurate.
- As a front-desk administrator, I want to deactivate a user who has left the organization, without losing their historical record.
- As a front-desk administrator, I want to reactivate a user who has returned.
- As a front-desk administrator, I want to search for a user by name or email so I can find them quickly in a large list.
- As a front-desk administrator, I want to see a paginated, sortable list of users so the screen stays responsive regardless of how many users exist.

## Business Rules

- A user's email address is unique across all (non-deleted) users.
- A newly created user defaults to **active** (`isActive = true`).
- A soft-deleted user is excluded from all normal list/search/get operations.
- Activating or deactivating a user does not delete or restore it — those are independent operations from soft delete.
- A soft-deleted user cannot be activated/deactivated or updated until restored (restoration is an explicit, privileged operation per [DatabaseArchitecture.md](../../DatabaseArchitecture.md) §6 — not implemented as a user-facing feature in this iteration).

## Validation Rules

| Field | Rule |
|---|---|
| `firstName` | Required, 1–100 characters |
| `lastName` | Required, 1–100 characters |
| `email` | Required, valid email format, unique, max 256 characters |
| `phoneNumber` | Optional, must match a basic phone-number pattern when provided |

Validation is enforced in two places, per [ApiStandards.md](../../ApiStandards.md) §7 and [FrontendArchitecture.md](../../FrontendArchitecture.md) §9:
- **Client-side** (web and mobile), via a shared schema in `frontend/shared/validation`, for immediate feedback.
- **Server-side** (authoritative), via FluentValidation in the backend's Application layer — the client check is a UX convenience only.

## Edge Cases

- Creating a user with an email that already exists (case-insensitive match) returns a `409 Conflict` with a business `errorCode`, not a generic `400`.
- Updating a user's email to one already used by another user is rejected the same way.
- Requesting a user by an ID that doesn't exist (or belongs to a soft-deleted user) returns `404 Not Found`.
- Activating an already-active user (or deactivating an already-inactive user) is treated as a no-op success, not an error — it is idempotent by design (see [ApiStandards.md](../../ApiStandards.md) §1).
- Searching with an empty result set returns a valid paginated envelope with an empty `data` array, not an error.
- Page numbers beyond the last page return an empty `data` array, not an error.

## Future Enhancements

- Add authentication fields (credential hash, last-login timestamp) once the Authentication iteration begins — as additive columns, not a redesign of this table.
- Add role/permission assignment once the Authorization iteration begins.
- Add a user-restoration (un-delete) admin feature.
- Add bulk import of users (e.g., CSV) once a real onboarding workflow is needed.
- Add profile photo upload, following [ApiStandards.md](../../ApiStandards.md) §10's file upload standards.

## API Reference

Base path: `/api/v1/users`. Every response uses the standard envelope/error shape from [ApiStandards.md](../../ApiStandards.md) §4–5.

| Method | Path | Purpose | Success | Failure |
|---|---|---|---|---|
| `POST` | `/api/v1/users` | Create a user | `201 Created` | `400` validation, `409` duplicate email |
| `PUT` | `/api/v1/users/{id}` | Update a user's profile | `200 OK` | `400` validation, `404` not found, `409` duplicate email |
| `DELETE` | `/api/v1/users/{id}` | Soft-delete a user | `204 No Content` | `404` not found |
| `GET` | `/api/v1/users/{id}` | Get a user by ID | `200 OK` | `404` not found |
| `GET` | `/api/v1/users` | Paged list — `page`, `pageSize`, `sort`, `search`, `isActive` query params | `200 OK` | — |
| `POST` | `/api/v1/users/{id}/activate` | Activate a user (idempotent) | `200 OK` | `404` not found |
| `POST` | `/api/v1/users/{id}/deactivate` | Deactivate a user (idempotent) | `200 OK` | `404` not found |

`errorCode` values this module produces: `IDENTITY.USER_NOT_FOUND`, `IDENTITY.USER_EMAIL_DUPLICATE`, `VALIDATION.FAILED`.

**Note:** every request currently records `createdBy`/`updatedBy`/`deletedBy` as `null` — there is no authenticated principal yet to attribute the action to. Wiring these to the real actor is a one-line change in `UsersController` once Authentication ships (see the `actorId: null` call sites and their TODO comments).

## Change History

| Date | Change |
|---|---|
| 2026-07-22 | Initial version — Users module built as the reference implementation for all future HMS modules. CRUD, soft delete, activate/deactivate, pagination/search/sort. Authentication, authorization, roles, and permissions explicitly deferred. |
| 2026-07-23 | Full implementation completed end-to-end: `identity.users` table + hand-authored migration, backend (entity, repository, service, validators, controller, DI, global exception handling), shared TypeScript package, React web feature, React Native mobile feature (full parity), and unit/integration/architecture tests. See [DecisionLog.md](../../DecisionLog.md) for the notable decisions made along the way. |
