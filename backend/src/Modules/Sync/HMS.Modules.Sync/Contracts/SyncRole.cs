namespace HMS.Modules.Sync.Contracts;

/// <summary>What this HMS instance does in the offline-first sync (ADR-133). Bound from
/// <c>Sync:Role</c>. The default, <see cref="None"/>, leaves the application exactly as it was
/// before the Sync module existed.</summary>
public enum SyncRole
{
    /// <summary>Sync is off: no outbox capture, no Hangfire, no receiver.</summary>
    None = 0,

    /// <summary>The on-site (local) server: captures patient changes into the outbox and pushes
    /// them to the cloud with Hangfire.</summary>
    Edge = 1,

    /// <summary>The cloud server: accepts authenticated sync batches from Edge servers.</summary>
    Cloud = 2,
}
