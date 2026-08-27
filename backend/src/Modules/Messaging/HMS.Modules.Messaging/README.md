# HMS.Modules.Messaging

Internal staff messaging — one-to-one and group conversations, read/unread state, history.

Layout: `Domain/`, `Application/`, `Infrastructure/`, `Contracts/`, `Endpoints/`.

Pairs with `HMS.Modules.Notifications` under the single `messages-and-notifications`
feature flag and the shared `engagement` permission category — see the design doc
("Messaging & Notification Module") for the full architecture. A new message to a
participant who isn't active in the conversation raises one in-app notification through
`HMS.Modules.Notifications`' public `INotificationService`, the same seam every other
module uses — this module never writes to another module's schema.

**Phase 1 status:** Domain entities (`Conversation`, `ConversationParticipant`, `Message`),
EF Core configuration, and repositories only. No `Application` services or `Endpoints`
controllers yet — those land in a later phase.
