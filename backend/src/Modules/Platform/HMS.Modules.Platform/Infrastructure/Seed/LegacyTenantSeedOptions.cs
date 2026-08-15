namespace HMS.Modules.Platform.Infrastructure.Seed;

/// <summary>
/// Bound from the "LegacyTenantSeed" configuration section — HMS Multi-Tenancy Phase C's
/// Phase 6 requirement: the pre-existing single dev database (ConnectionStrings:Default)
/// predates tenant-aware JWTs and needs a real platform.tenants row so its existing users
/// can still log in (via HospitalCode) and receive a valid TenantId claim. Unlike
/// PlatformAdminSeedOptions, every field has a working default — this seed must succeed for
/// the legacy database to remain reachable at all, so it can't silently no-op on missing
/// config the way the optional Platform Admin seed does.
/// </summary>
internal sealed class LegacyTenantSeedOptions
{
    public const string SectionName = "LegacyTenantSeed";

    public string HospitalCode { get; set; } = "legacy";

    public string HospitalName { get; set; } = "Legacy Hospital";

    public string MobileNumber { get; set; } = "0000000000";

    public string Email { get; set; } = "legacy-tenant@hms.local";

    public string Address { get; set; } = "Not applicable";

    public string City { get; set; } = "Not applicable";

    public string State { get; set; } = "Not applicable";

    public string Pincode { get; set; } = "000000";
}
