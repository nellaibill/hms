namespace HMS.Modules.Platform.Contracts;

public enum TenantStatus
{
    Active,
    Inactive,
}

/// <summary>
/// A Platform Admin's privilege level. SuperAdmin can perform every action, including the
/// destructive/high-privilege ones (register a hospital, enable/disable a hospital, trigger a
/// tenant migration). SupportUser is read-only — dashboard/list access only — until a real
/// per-tenant permission model exists (see docs/DecisionLog.md ADR-014).
/// </summary>
public enum PlatformRole
{
    SuperAdmin,
    SupportUser,
}
