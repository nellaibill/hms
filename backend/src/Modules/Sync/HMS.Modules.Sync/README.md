# HMS.Modules.Sync

Offline-first patient sync from a hospital's local (Edge) server to the cloud HMS: a
same-transaction outbox, Hangfire jobs, an API-key-protected cloud receiver with EventId
idempotency and conflict detection, and the monitoring API behind `/admin/cloud-sync`.

Off by default (`Sync:Role = None`). Design, configuration, runbook and test record:
[docs/modules/Sync/OfflineFirstSync.md](../../../../../docs/modules/Sync/OfflineFirstSync.md) (ADR-133).
