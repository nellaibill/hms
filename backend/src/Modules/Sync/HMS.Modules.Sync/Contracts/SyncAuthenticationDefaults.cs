namespace HMS.Modules.Sync.Contracts;

/// <summary>
/// The cloud-side authentication scheme for Edge servers (ADR-133). An Edge server presents
/// <c>Authorization: HmsSync &lt;key&gt;</c>; the key is looked up by its SHA-256 hash in
/// sync_registry.sync_clients. No user token is involved, so these requests never reach any
/// user-facing endpoint: only the sync receiver accepts this scheme.
/// </summary>
public static class SyncAuthenticationDefaults
{
    public const string Scheme = "HmsSync";

    /// <summary>Authorization policy the receiver endpoints require.</summary>
    public const string Policy = "SyncClient";

    public const string SyncClientIdClaim = "SyncClientId";
    public const string TenantIdClaim = "TenantId";
    public const string HospitalCodeClaim = "HospitalCode";

    /// <summary>True when the Authorization header uses this scheme. Used by
    /// TenantResolutionMiddleware to resolve the hospital for a sync request.</summary>
    public static bool IsSyncAuthorizationHeader(string? authorizationHeader) =>
        authorizationHeader is not null
        && authorizationHeader.StartsWith(Scheme + " ", StringComparison.OrdinalIgnoreCase);
}
