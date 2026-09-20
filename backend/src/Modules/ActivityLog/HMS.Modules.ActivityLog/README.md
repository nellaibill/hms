# HMS.Modules.ActivityLog

Centralized, append-only audit trail (schema `activity_log`, table `activity_logs`).

Layout: `Domain/`, `Application/`, `Infrastructure/`, `Contracts/`, `Endpoints/`.

- **Write seam:** `IActivityLogService.LogAsync(ActivityLogRequest)` (public, `Application/`). Fills
  TenantId, UserId, IP, User-Agent and CorrelationId from the request context; never throws (an
  audit failure must not change the audited operation). Old/New values are serialized to JSON and
  scrubbed of password/token/secret-like properties (`ActivityLogSanitizer`).
- **Read API:** `GET /api/v1/activity-logs` (paged; from/to, userId, module, action, entityType,
  entityId, search) and `GET /api/v1/activity-logs/{id}`. Requires `identity-administration.view`
  and the `activity-log` feature (Mandatory). No update/delete anywhere.
- **Callers:** Patients (create/update/delete), Identity (user create/update/delete, role
  create/update/delete), Billing (invoice create, payment, void).
