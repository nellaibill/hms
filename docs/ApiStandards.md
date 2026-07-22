# API Standards

## Purpose
This document defines the API standards every backend module and frontend application (web and mobile) must follow, so consuming or extending any part of the API is predictable regardless of which module implements it. It is a standards document — it contains no controllers, endpoints, DTOs, or source code.

## Scope
Covers REST design principles, URL conventions, request/response shape, error format, pagination, validation, and the API-contract-level standards for authentication and authorization (headers, token handling expectations, claims shape). File upload, date/time, versioning, and documentation standards are included as they are cross-cutting API concerns.

**Out of scope:** the underlying authentication mechanism and token issuance (see [Authentication.md](Authentication.md)), the underlying role/permission model (see [Authorization.md](Authorization.md)), and the global exception-handling implementation (see [ErrorHandling.md](ErrorHandling.md)) — this document defines the *contract* those systems must expose at the API boundary.

## When to Update This Document
- A new cross-cutting API convention is adopted (e.g., a new pagination or filtering pattern).
- The API versioning strategy changes.
- The standard error or response envelope shape changes (coordinate with [ErrorHandling.md](ErrorHandling.md)).
- A new cross-cutting header or authentication/authorization contract detail is introduced.

## Recommended Sections
- API Design Principles
- URL Standards
- Request Standards
- Response Standards
- Error Response Standard
- Pagination
- Validation Standards
- Authentication
- Authorization
- File Upload Standards
- Date & Time Standards
- API Versioning Strategy
- API Documentation

---

## 1. API Design Principles

**REST conventions.** Every module exposes its capability as a set of resources manipulated through standard HTTP verbs — not as remote-procedure-call-style action endpoints. The API is stateless: no server-side session is held between requests, and every request carries whatever context (auth token, correlation id) it needs to be handled independently.

**Resource naming.** Resources are plural nouns (`patients`, `appointments`), never verbs (`getPatients`, `createAppointment`). Multi-word resource names use lowercase `kebab-case` in the URL (`lab-results`, not `labResults` or `lab_results`).

**URI conventions.** Path segments are lowercase `kebab-case`; JSON body and response field names are `camelCase` (matching .NET's default JSON serialization behavior, so the frontend gets idiomatic naming without a translation layer). No trailing slashes, no file extensions in the URL, no verbs in the path — the HTTP method carries the verb.

**HTTP method usage.**

| Method | Use | Idempotent |
|---|---|---|
| `GET` | Read a resource or collection. No body, no side effects. | Yes |
| `POST` | Create a resource, or trigger a non-idempotent action. | No |
| `PUT` | Replace a resource in full. | Yes |
| `PATCH` | Partially update a resource. | Should be designed to be, where practical |
| `DELETE` | Remove (or soft-delete) a resource. | Yes |

**Idempotency.** `GET`, `PUT`, and `DELETE` must be safe to retry without changing the outcome beyond the first successful call. `POST` is inherently non-idempotent, but for operations where a client retry after a timeout could cause harmful duplication (e.g., creating a billing charge), the endpoint supports an `Idempotency-Key` request header (see §3) so a retried request with the same key returns the original result instead of creating a duplicate.

**Consistency guidelines.** Every module's endpoints follow the same casing, envelope, pagination, and error conventions defined in this document. A frontend developer should never need to special-case one module's response shape versus another's — consistency here is what makes the modular monolith feel like one coherent API rather than six independently designed ones.

---

## 2. URL Standards

Examples below are illustrative patterns only — no actual endpoints are defined by this document.

**Versioning.** The API version is a path segment: `/api/v1/{resource}`. See §12 for the full versioning and deprecation policy.

**Resource hierarchy.** Collections and instances follow a consistent pattern:
- Collection: `/api/v1/{resource}`
- Single instance: `/api/v1/{resource}/{id}`

**Nested resources.** Nesting is used only when the child resource cannot exist independently of its parent (true ownership) — e.g., `/api/v1/patients/{patientId}/appointments` to view appointments in the context of a specific patient. If a resource has meaning or access needs outside that parent context, prefer a top-level resource filtered by query parameter instead: `/api/v1/appointments?patientId={id}`. Nesting is kept to a single level — deeper nesting produces brittle URLs that are hard to evolve.

**Query parameters.** Used only for filtering, sorting, searching, and pagination — never for identifying a specific resource (that is the path's job). Example: `/api/v1/appointments?status=scheduled&sort=-scheduledAt&page=1&pageSize=20`.

---

## 3. Request Standards

| Header | Standard |
|---|---|
| `Content-Type` | `application/json` required on any request with a body. No other content types are accepted for MVP. |
| `Accept` | `application/json` expected; the API returns JSON only. |
| `Authorization` | `Bearer {token}` — required on every endpoint except explicitly public ones (e.g., login). See §8. |
| `X-Correlation-Id` | A correlation identifier for the request, propagated to the response and to every log entry generated while handling it (see [Logging.md](Logging.md)). If a client does not supply one, the API generates one and returns it in the response headers so it can still be used for support/debugging. |
| `Idempotency-Key` | Optional; recommended for unsafe `POST` operations where a retried request must not create a duplicate (see §1). |
| `Accept-Language` | Reserved for future localization. Not required for MVP (single-language assumption), but the API accepts and ignores it rather than erroring, so localization can be added later without a breaking change. |

---

## 4. Response Standards

Every response uses a consistent envelope. This is a **pattern**, not an implementation:

```
{
  "data": { } or [ ],
  "meta": { },
  "messages": [ ]
}
```

- **`data`** — present on every successful response that returns a resource or collection; a single object for a single-resource response, an array for a collection. Omitted (or the response has no body at all) for actions with no content to return, such as a `204 No Content` delete.
- **`meta`** — present whenever the response carries information *about* the data beyond the data itself. Most commonly this is pagination metadata (see §6), but it may also echo back applied filters/sort for client confirmation. Omitted when there's nothing meaningful to report — a single-resource `GET` typically has no `meta`.
- **Pagination** (nested within `meta`) — present only on collection/list endpoints; carries page number, page size, total count, and total pages (see §6 for the parameter standard).
- **`messages`** — present when the API needs to communicate something beyond the raw data, such as a non-fatal warning or an informational note (e.g., "this record was merged with an existing one"). Not used for validation or error details, which have their own dedicated shape (§5). Omitted in the common case where there is nothing to report beyond the data itself.

**Rule of thumb:** nothing in the envelope is mandatory except `data` on a successful response with content — the standard should never force empty/null boilerplate onto a response that has nothing to say.

---

## 5. Error Response Standard

Every error response — regardless of which module or layer produced it — uses one consistent shape, produced by the single global exception handler at the host (see [ErrorHandling.md](ErrorHandling.md)), never hand-rolled per endpoint:

```
{
  "errorCode": "string",
  "message": "string",
  "validationErrors": [
    { "field": "string", "message": "string" }
  ],
  "correlationId": "string",
  "timestamp": "ISO-8601 UTC"
}
```

- **`errorCode`** — a stable, machine-readable, namespaced code (not the HTTP status code) that frontend code can branch on without parsing message text. Message text may be reworded or localized later; error codes should not change once published.
- **`message`** — a human-readable summary suitable for logs and for display to technical users. Not necessarily the exact copy shown in end-user UI — the frontend may map `errorCode` to localized, user-friendly text.
- **`validationErrors`** — present only for input-validation failures (§7); an array of field-level messages so the frontend can highlight the specific offending field(s). Absent for any other error type (`404`, `401`, `500`, etc.).
- **`correlationId`** — always present, matches the request's `X-Correlation-Id` (§3), so a user-reported error can be traced directly to the corresponding server-side log entries.
- **`timestamp`** — always present, ISO-8601, UTC (§11), generated at the moment the error was produced.

---

## 6. Pagination

| Concern | Standard |
|---|---|
| Page Number | Query parameter `page`, 1-based (`page=1` is the first page, never `0`). |
| Page Size | Query parameter `pageSize`, with a sane server-defined default and an enforced server-side maximum — a client cannot request an unbounded page size. |
| Sorting | Query parameter `sort`; a field name optionally prefixed with `-` for descending (`sort=-scheduledAt`). Multiple fields via comma-separation (`sort=lastName,-createdAt`) where an endpoint genuinely needs multi-field sort — most need only one. |
| Filtering | Query parameters named after the field being filtered (`status=scheduled`), combined as an implicit AND. Ranges use suffixed parameter names (`scheduledAfter`, `scheduledBefore`) rather than a generic operator syntax, keeping filters simple and self-documenting at MVP scope. |
| Searching | A dedicated `search` (or `q`) query parameter for free-text search across an endpoint's designated searchable fields — distinct from exact-match filters. Matching behavior (prefix, substring, fuzzy) is documented per endpoint since it depends on the underlying index strategy (see [DatabaseArchitecture.md](DatabaseArchitecture.md) §8). |

---

## 7. Validation Standards

- **Input validation** — shape, format, and required-field checks run at the API boundary before any business logic executes (see [ErrorHandling.md](ErrorHandling.md)'s two-level validation strategy). Failures return `400` with the `validationErrors` shape from §5.
- **Business validation** — domain-rule checks that require application/domain knowledge (state transitions, uniqueness, cross-field or cross-entity rules). These may return a more specific status (`409 Conflict` for state conflicts, `422 Unprocessable Entity` for a well-formed but semantically invalid request) — the standard that matters is that the response still uses the same error envelope from §5, with an `errorCode` reflecting the business rule rather than a formatting problem.
- **Validation error responses** — always return every validation failure found in a single pass, not one at a time, so a caller can fix all issues before resubmitting instead of receiving sequential `400`s.

---

## 8. Authentication

This section defines the API-contract standards every endpoint must honor. The underlying token issuance and validation mechanism is designed in [Authentication.md](Authentication.md).

- **JWT** — the API is a stateless JWT bearer-token consumer. Every authenticated request carries `Authorization: Bearer {token}`.
- **Refresh Tokens** — a separate, longer-lived credential used only against a dedicated refresh endpoint to obtain a new access token. Never sent as the `Authorization` header on ordinary API calls, and never placed anywhere more exposed than necessary (see secure storage below).
- **Token Expiration** — access tokens are short-lived by design, so a leaked token has a bounded blast radius. Clients handle a `401` by attempting a token refresh, not by immediately forcing re-login, unless the refresh itself fails.
- **Secure Storage** — no client stores a token anywhere a browser extension, injected script, or unencrypted file read could trivially retrieve it. Web clients avoid storing tokens in a way exposed to XSS; mobile clients use the platform's secure keystore. The exact mechanism is Authentication.md's responsibility — this document's standard is the outcome, not the implementation.
- **Authorization Header** — exactly one bearer token per request via the standard `Authorization: Bearer {token}` header. No custom auth headers, and tokens are never passed as query parameters (query strings leak into logs, proxies, and browser history).

---

## 9. Authorization

This section defines the API-contract standards for access control. The underlying role/permission model is designed in [Authorization.md](Authorization.md).

- **Roles** — a coarse-grained grouping of permissions assigned to a user (e.g., front-desk staff vs. clinician vs. administrator), defined once in the Identity module and referenced consistently by every other module's authorization checks.
- **Permissions** — fine-grained, per-action rights (e.g., "view billing records" vs. "issue a refund"). Endpoint authorization is expressed against permissions, not hard-coded role names, so role definitions can change without touching every endpoint.
- **Policy-based authorization** — ASP.NET Core policy-based authorization is the standard mechanism: an endpoint declares which policy it requires, and the policy's definition (which roles/permissions/claims satisfy it) lives in one place, not as ad hoc role-name checks scattered across modules.
- **Claims** — the JWT carries the claims a policy evaluates (user id, role(s), and any permission claims needed). Modules read claims through a shared current-user abstraction rather than parsing the token directly, so the claim shape can evolve without every module needing to change.

---

## 10. File Upload Standards

- **Upload endpoints** — a dedicated endpoint per upload use case (e.g., a lab-report attachment upload) rather than one generic file-upload endpoint for everything, so validation, storage location, and authorization stay specific and explicit per use case.
- **Validation** — every upload validates content type and the actual file signature, not just the filename extension (which is trivially spoofable).
- **File size** — every upload endpoint enforces an explicit maximum size appropriate to its use case. No endpoint accepts an unbounded upload.
- **Allowed formats** — each upload endpoint documents and enforces an explicit allow-list of accepted formats. Reject anything outside the allow-list rather than trying to maintain a deny-list of dangerous types.

Uploaded files are treated as untrusted input: stored outside the web root, never executed, and served back (if at all) through a controlled download endpoint rather than direct static file serving — consistent with [Security.md](Security.md).

---

## 11. Date & Time Standards

- **UTC storage** — every timestamp is stored in the database in UTC (see [DatabaseArchitecture.md](DatabaseArchitecture.md)'s audit columns). There is exactly one time zone in storage, eliminating an entire class of ambiguity bugs.
- **ISO-8601 format** — every date/time value crossing the API boundary, in either direction, is formatted as ISO-8601 with an explicit UTC designator. No locale-specific or ambiguous date formats are ever sent or accepted over the API.
- **Time zone handling** — conversion to a user's local time zone happens at the presentation layer (web/mobile client) only, never on the server or in storage. If a feature genuinely needs a facility's local time (e.g., "business hours"), that time zone is stored as explicit configuration data, not inferred from server or client environment.

---

## 12. API Versioning Strategy

- **URL versioning** — the version is a path segment (`/api/v1/...`), chosen over header-based or content-negotiation-based versioning because it is visible, cacheable, easy to route, and easy for a small team (including junior developers) to reason about without inspecting headers.
- **Deprecation policy** — a version is marked deprecated with advance notice (documentation plus a deprecation-signaling response header) before it is removed. Consumers get a defined migration window before the old version stops being served.
- **Backward compatibility** — within a single version, changes must be additive and backward-compatible (new optional fields, new endpoints). Anything that would break an existing client — removing or renaming a field, changing a field's type or meaning, changing status codes — requires a new version rather than a silent change to the current one.

---

## 13. API Documentation

- **OpenAPI** — every endpoint is described by an OpenAPI specification generated from the API project itself, not hand-maintained separately, so documentation cannot silently drift from the actual implementation.
- **Swagger** — an interactive Swagger UI (or equivalent) is exposed in non-production environments for exploration and manual testing. Production exposure is a deliberate, reviewed decision, not a default (see [Security.md](Security.md)).
- **XML comments** — public-facing endpoint and DTO members carry XML documentation comments so the generated OpenAPI spec includes meaningful descriptions rather than bare type/field names. Documentation lives with the code, not in a separate document that can go stale.
- **Examples** — every documented endpoint includes at least one representative request/response example in its OpenAPI description, so a frontend developer integrating against it doesn't need to reverse-engineer the shape from source code.

---

No controllers, endpoints, DTOs, or business APIs were generated — this is a standards document only.
