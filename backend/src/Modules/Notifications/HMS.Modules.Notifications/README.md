# HMS.Modules.Notifications

Notification templates, per-user channel preferences, in-app/email/SMS fan-out, and
per-channel delivery tracking. Pairs with `HMS.Modules.Messaging` under the single
`messages-and-notifications` feature flag and the shared `engagement` permission
category — see the design doc ("Messaging & Notification Module") for the full
architecture.

Layout: `Domain/`, `Application/`, `Infrastructure/`, `Contracts/`, `Endpoints/`.

Every other module reaches this one through a single public seam,
`INotificationService` (added in a later phase) — never by depending on this module's
`Domain`/`Application`/`Infrastructure` directly.

**Phase 1 status:** Domain entities (`Notification`, `NotificationRecipient`,
`NotificationDelivery`, `NotificationTemplate`, `NotificationPreference`), EF Core
configuration, and repositories only. No `Application` services or `Endpoints`
controllers yet — those land in a later phase.
