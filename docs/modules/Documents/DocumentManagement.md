# Document Management Module

This document is the business and functional reference for the Documents module — a generic, cross-module document repository usable by every HMS module (Patient, Staff, Doctor, Appointment, Admission, Lab, Radiology, Billing, Asset, Vendor), keyed only by owner type + owner id. It follows the documentation shape established by [Users.md](../Identity/Users.md).

## Module Purpose

The Documents module owns file upload, storage, retrieval, archiving, and soft deletion for any record in the system, inside the `documents` PostgreSQL schema. It replaces the UI-only mock that shipped in `frontend/web/src/features/documents` (PR #40) with a real backend, implementing the nine user stories from the architecture review (see below).

**Relationship to other "document" concepts in this codebase** — this was the single largest open question going into this module and had no ADR before now:

- **Patients' own photo/ID-proof upload** (`PatientsController.UploadPhoto`/`UploadIdProof`) is a separate, pre-existing, real flow that stores files under `wwwroot/uploads/patients` and is **not** migrated to this module in this iteration. The two now coexist. Unifying them (routing Patients' uploads through this module's `IDocumentFileStorage`/`DocumentsController` instead) is flagged as follow-up work, not done here, to avoid changing Patients' registration flow as a side effect of this module.
- **E-MRD** (`/records/emrd`) and **Records and Certificates** (`/records/certificates`) are frontend nav placeholders with no backend of their own. The intended relationship — E-MRD as a filtered view over this module scoped to Patient + clinical document types, Records and Certificates as a workflow that produces documents into this same store — is a design recommendation, not yet implemented.

**Explicitly out of scope for this iteration:**
- Document versioning (a re-upload creates an unrelated new row, not a linked version).
- OCR, full-text search, digital signature, watermarking, retention-policy automation.
- A dynamic, database-backed permission/classification matrix (see Authorization below).
- Existence validation for six of the ten owner types (see Authorization/Data Model).

## User Stories

Implemented, priority-ordered (see the originating architecture review for full acceptance criteria):

- **US-1 (Critical)** — As a front-desk/clinical/records staff member, I want to upload a file and attach it to a specific patient, staff member, appointment, or other HMS record, so that supporting documents live in one governed repository instead of scattered local files.
- **US-2 (Critical)** — As a compliance-conscious hospital administrator, I want document access limited by the requester's role and the document's owner type/classification, so that non-clinical staff can't browse patient consent forms and non-financial staff can't browse vendor invoices.
- **US-3 (High)** — As records/MRD staff, I want to search and filter the document repository by owner type, entity, document type, uploader, date range, and status, so that I can find a specific file without scrolling an unbounded list.
- **US-4 (Critical)** — As an authorized user, I want to preview or download a document I have permission to see, so that I can act on it without the file being reachable by anyone who guesses its URL.
- **US-5 (High)** — As records staff, I want to archive an outdated document so it's hidden from active searches but still retrievable, so that historical records aren't confused with current ones but nothing is lost.
- **US-6 (High)** — As a compliance officer, I want a "deleted" document to be soft-deleted, so that an audit can still discover it existed even after a user removes it from view.
- **US-7 (Medium)** — As an administrator, I want summary counts (total, uploaded today, archived, storage used) computed server-side, so that the dashboard stays fast regardless of how many documents exist.
- **US-8 (Medium)** — As a compliance officer, I want every view, download, and deletion of a document logged with who did it and when, so that I can answer "who has seen this patient's consent form."
- **US-9 (Medium, fast-follow-shaped)** — As a security-conscious operator, I want every uploaded file scanned for malware before it's marked available for download, so that the repository can't be used to distribute malicious files to staff.

## Domain Model

`Document` (aggregate root, `documents.documents`): `Id`, `OwnerType`, `OwnerId`, `DocumentType`, `Classification`, `StorageKey`, `OriginalFileName`, `ContentType`, `SizeBytes`, `ChecksumSha256`, `Status`, `IsArchived`, `UploadedByUserId`, plus the standard audit tail (`CreatedAt/By`, `UpdatedAt/By`, `IsDeleted/DeletedAt/By`) and `xmin` concurrency token — the same shape as `HMS.Modules.Patients.Domain.Patient`.

`OwnerType`, `DocumentType`, `Classification`, and `Status` are closed enums (stored as their string name), declared in `Contracts` — not `Domain` — because they're exposed as public DTO properties and a public property can't reference a less-visible type; this mirrors `HMS.Modules.Patients.Contracts.PatientEnums.cs`.

`Classification` (Public/Internal/Confidential/Restricted) is deliberately independent of `OwnerType`/`DocumentType`: a Vendor invoice and a patient consent form have very different exposure even though neither is inherently more "Patient-shaped" than the other. `DocumentAccessPolicy` uses it for one piece of classification-driven gating (Restricted documents require an elevated role regardless of owner-type access).

`Status` (Pending/Available/Quarantined) is the state the asynchronous scan pipeline (US-9) drives — content is only downloadable once `Available`.

**No polymorphic foreign key exists** — `owner_id` is a plain `uuid` column with no database-level constraint tying it to a specific parent table, because no other table in this codebase has ever needed a polymorphic association and introducing one (via a trigger or generalized constraint) would be disproportionate to this one module. Referential integrity is enforced at the application layer instead — see Authorization/Data Integrity below.

## Authorization (US-2)

There is no ASP.NET Core policy-based authorization infrastructure anywhere in this codebase as of this module's creation — `AddAuthorization()` is called with framework defaults, and the only prior `[Authorize]` usage (`AuthenticationController.Me()`) is a bare authentication-only gate. `DocumentsController` is the **first** controller in the codebase to require authentication for every action and to derive a real actor from JWT claims instead of passing `actorId: null`.

`DocumentAccessPolicy` (`Application/Security/`) is a pragmatic, explicitly-scoped middle ground: an in-code table mapping the stable `LoginType` JWT claim (`HMS.Modules.Identity.Application.LoginTypes` — not the freeform, admin-creatable `RoleName` claim) to the `DocumentOwnerType` values that role may read/write, plus a small allow-list of roles permitted to touch `Restricted`-classification documents. `superAdmin`/`admin` bypass every check, mirroring `frontend/web/src/config/navigation.ts`'s treatment of those two roles.

This is a real, enforced check today — not a placeholder — but it is coarser than the dynamic, database-backed Permission/RolePermission-driven policy model `docs/ApiStandards.md` §9 describes as the standard. Standing that up is a platform-wide undertaking affecting every module's controllers, not something this module should do unilaterally; `DocumentAccessPolicy`'s interface is the seam to swap in a database-backed implementation once that work happens everywhere, not just here.

## Data Integrity: Owner Existence Validation (US-1)

Only **Patient** has a real backend module and a registered `IDocumentOwnerExistenceChecker` as of this module's creation (`HMS.Modules.Patients.Infrastructure.PatientDocumentOwnerExistenceChecker`). Staff, Doctor, Appointment, Admission, Lab, Radiology, Billing, Asset, and Vendor either have no backend module at all (Doctor, Admission, Lab, Radiology, Asset, Vendor) or have one that wasn't wired up in this iteration (Staff, Appointment, Billing — deferred to keep this change's footprint to what could be verified end-to-end against real reference code, rather than guessed against modules not inspected while building this).

`DocumentService.UploadAsync` resolves a checker by owner type; if none is registered, the upload is **not rejected** — it proceeds, and a warning is logged (`No owner-existence checker registered for {OwnerType}...`). This is a conscious choice: silently pretending to validate nine module boundaries that don't exist yet would be worse than being honest that they aren't validated. As each additional module gets a real backend, register an `IDocumentOwnerExistenceChecker` implementation for it the same way `PatientDocumentOwnerExistenceChecker` does, and this gap closes one owner type at a time with no changes needed to `DocumentService` itself.

## File Storage & Content Delivery (US-1, US-4)

Local disk under `App_Data/documents` (outside `wwwroot`), mirroring `HMS.Modules.Patients.Infrastructure.PatientFileStorage`'s "no premature complexity" rationale — no blob storage/CDN until there's a real need. The one deliberate difference from Patients' pattern: this storage root is **never** served via `app.UseStaticFiles()`. A document's bytes are reachable only through the authenticated `GET /api/v1/documents/{id}/content` action, which re-runs the same access check as every other read before streaming — per `docs/ApiStandards.md` §10 ("served through a controlled download endpoint rather than direct static file serving"). Files are stored under the document's own id as the filename, so a crafted upload filename can't traverse or overwrite an unrelated path on disk. SHA-256 checksums are computed in the same pass as the disk write, not a second read.

## Virus Scanning (US-9)

`IDocumentScanQueue` (an in-process, bounded `System.Threading.Channels.Channel<Guid>`) plus `DocumentScanBackgroundService` (an `IHostedService`) form the platform's **first** background-job mechanism — nothing like Hangfire or Quartz exists elsewhere in this codebase, and introducing one was judged disproportionate to this one pipeline. Every upload is queued immediately after being persisted and starts in `Pending` status; the background service scans it and transitions it to `Available` or `Quarantined`.

**`IVirusScanner`'s registered implementation, `NullVirusScanner`, always reports Clean.** No real antivirus engine (ClamAV or otherwise) is integrated. This is stated plainly rather than silently: the pipeline's plumbing (queueing, status transitions, the authenticated-content-gate-on-`Available` behavior) is real and exercised today, but a document reaching `Available` is not evidence it was actually scanned for malware while `NullVirusScanner` is registered. Replacing it with a real engine is a single-class swap (see Risks).

Being in-memory, a queued item is lost if the process restarts before being drained — the document simply stays `Pending` forever rather than being silently marked `Available`, which was judged an acceptable MVP trade for this specific pipeline; it is not a substitute for a durable queue if this pipeline grows higher-stakes responsibilities later.

## Business Rules

- A document's `OwnerId` must reference an existing record of the given `OwnerType` wherever that can be checked (see Data Integrity above).
- Uploads are rejected (400) if the file is empty, exceeds the configured maximum (`Documents:MaxFileSizeMb`, default 10MB), has an unsupported content type, or if its actual byte signature doesn't match its declared content type.
- A document's content is downloadable only when `Status == Available`; `Pending` and `Quarantined` both surface as "not available yet" (409) to the caller — only summary/admin tooling needs to distinguish the two.
- Archive is idempotent — archiving an already-archived document is a no-op success.
- Delete is always soft (`IsDeleted`/`DeletedAt`/`DeletedBy`) — the file on disk is retained, not physically removed, so a compliance audit can still answer "what was here before it was deleted." A retention/purge job is future work.
- A caller who can't see a document (per `DocumentAccessPolicy`) gets the same 404 as a genuinely missing document, never a 403 that would confirm the record's existence.

## Validation Rules

| Field | Rule |
|---|---|
| `OwnerType` | Required, must be a known `DocumentOwnerType` value |
| `OwnerId` | Required (non-empty GUID) |
| `DocumentType` | Required, must be a known `DocumentType` value |
| `Classification` | Optional, defaults to `Internal`; must be a known value when supplied |
| File | Required, non-empty, ≤ configured max size, content type in the allow-list, byte signature matches declared content type |

Server-side only — this module has no corresponding frontend integration yet (see Future Enhancements); the existing frontend mock's client-side checks in `frontend/web/src/features/documents/validation.ts` are not wired to this API.

## Edge Cases

- Uploading against an `OwnerId` that doesn't exist (for an owner type with a registered checker) returns `404` with `DOCUMENTS.OWNER_NOT_FOUND`.
- Requesting a document's metadata or content that the caller can't see (per `DocumentAccessPolicy`) returns the same `404`/`DOCUMENTS.DOCUMENT_NOT_FOUND` as a document that doesn't exist at all.
- Requesting content for a `Pending` or `Quarantined` document returns `409`/`DOCUMENTS.CONTENT_NOT_AVAILABLE`.
- Archiving an already-archived document, or re-issuing the same delete, succeeds idempotently rather than erroring.
- An owner type with no registered `IDocumentOwnerExistenceChecker` is accepted (with a logged warning), not rejected — see Data Integrity above.

## API Reference

Base path: `/api/v1/documents`. Every action requires authentication (`[Authorize]`). Responses use the standard envelope/error shape from `docs/ApiStandards.md` §4–5.

| Method | Path | Purpose | Success | Failure |
|---|---|---|---|---|
| `POST` | `/api/v1/documents` | Upload a document (multipart/form-data) | `201 Created` | `400` validation/invalid file, `403` forbidden, `404` owner not found |
| `GET` | `/api/v1/documents` | Paged/filtered list — `ownerType`, `ownerId`, `documentType`, `uploadedByUserId`, `dateFrom`, `dateTo`, `status`, `search`, `page`, `pageSize`, `sort` | `200 OK` | — |
| `GET` | `/api/v1/documents/summary` | Server-side KPI aggregate | `200 OK` | — |
| `GET` | `/api/v1/documents/{id}` | Get a document's metadata | `200 OK` | `404` not found/not visible |
| `GET` | `/api/v1/documents/{id}/content` | Stream a document's bytes (range-request capable) | `200 OK` | `404` not found/not visible, `409` not available yet |
| `PATCH` | `/api/v1/documents/{id}/archive` | Archive (idempotent) | `200 OK` | `403` forbidden, `404` not found/not visible |
| `DELETE` | `/api/v1/documents/{id}` | Soft-delete | `204 No Content` | `403` forbidden, `404` not found/not visible |

`errorCode` values this module produces: `DOCUMENTS.DOCUMENT_NOT_FOUND`, `DOCUMENTS.INVALID_FILE`, `DOCUMENTS.OWNER_NOT_FOUND`, `DOCUMENTS.FORBIDDEN`, `DOCUMENTS.CONTENT_NOT_AVAILABLE`, `VALIDATION.FAILED`.

**Not implemented in this iteration:** bulk/batch upload (a realistic MRD paper-scanning workflow, flagged as a near-term fast-follow, not a hypothetical), document versioning endpoints.

## Risks

- **`NullVirusScanner` provides no real malware protection.** Anyone relying on `Status == Available` as a scanning guarantee today is relying on a stub. Tracked as the top item to close before any real, external-facing deployment.
- **Six of ten owner types have no existence validation.** An upload against `Doctor`, `Admission`, `Lab`, `Radiology`, `Asset`, or `Vendor` (and, until wired up, `Staff`/`Appointment`/`Billing`) is accepted without confirming the target record exists.
- **`DocumentAccessPolicy`'s role-to-owner-type map is an in-code table, not admin-configurable.** Changing which roles can see which owner types requires a code change and redeploy, not a UI action.
- **The frontend mock is not wired to this API.** `frontend/web/src/features/documents` still runs entirely against `localStorage`; connecting it is separate follow-up work, not part of this change.
- **Patients' own upload flow was not consolidated into this module.** Two independent document-upload code paths now exist for the same conceptual capability.

## Future Enhancements

- Wire `frontend/web/src/features/documents` to this real API in place of its `localStorage` mock.
- Register `IDocumentOwnerExistenceChecker` implementations for Staff, Appointment, and Billing (real modules that just weren't wired up yet), then for Doctor/Admission/Lab/Radiology/Asset/Vendor as their own backend modules come online.
- Replace `NullVirusScanner` with a real engine (e.g. ClamAV) — the interface seam is already in place.
- Consolidate Patients' `UploadPhoto`/`UploadIdProof` into this module once a migration path for existing uploaded files is designed.
- Resolve E-MRD and Records & Certificates against this module per the relationship described above.
- Document versioning, full-text search/OCR, digital signature, retention-policy automation, bulk upload, a database-backed classification/permission matrix once that infrastructure exists platform-wide.
