namespace HMS.Shared.Infrastructure;

/// <summary>Names of the hospital-side authorization policies registered in
/// HMS.Api.Configuration.JwtConfiguration, for use as <c>[Authorize(Policy = ...)]</c>.</summary>
public static class HospitalPolicies
{
    /// <summary>A hospital user signed in as Super Admin (ADR-150) — for screens only the
    /// hospital's top admin may open, such as the Activity Log, whatever permissions other
    /// roles hold.</summary>
    public const string SuperAdmin = "HospitalSuperAdmin";
}
