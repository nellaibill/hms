namespace HMS.Modules.Notifications.Contracts;

/// <summary>Entity-owned enum kept in Contracts (not Domain) because it's also referenced
/// by DTOs added in a later phase — mirrors HMS.Modules.Pharmacy's Contracts/
/// PharmacyEnums.cs convention (e.g. TransactionType).</summary>
public enum NotificationChannel
{
    InApp,
    Email,
    Sms,
}

/// <summary>Emergency notifications bypass NotificationPreferences entirely (every channel,
/// always) — see NotificationPreference's own doc comment.</summary>
public enum NotificationSeverity
{
    Normal,
    Emergency,
}

/// <summary>Tracks one NotificationDelivery row's lifecycle. In-app delivery has no
/// corresponding row (see NotificationDelivery's doc comment) — this status only ever
/// applies to Email/Sms, which is why Pending/Failed/Skipped matter: those channels can
/// fail or be skipped by preference in a way "the row exists" never captures.</summary>
public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
}
