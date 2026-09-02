namespace HMS.Modules.Platform.Contracts;

public record CreateHospitalRequest
{
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;

    public string SuperAdminUsername { get; init; } = string.Empty;
    public string SuperAdminFirstName { get; init; } = string.Empty;
    public string SuperAdminLastName { get; init; } = string.Empty;
    public string SuperAdminEmail { get; init; } = string.Empty;
    public string SuperAdminPhoneNumber { get; init; } = string.Empty;
    public string SuperAdminPassword { get; init; } = string.Empty;

    /// <summary>Optional FeatureCatalog keys to enable for this tenant beyond
    /// FeatureCatalog.Mandatory (which is always included regardless of this list) — drives
    /// which schemas get provisioned (see Tenant Feature/Module Management). An empty list
    /// provisions only the mandatory modules.</summary>
    public IReadOnlyList<string> EnabledFeatureKeys { get; init; } = [];

    /// <summary>UHIDs 1 through this number are reserved for this hospital's bulk-imported/
    /// legacy patients; new manual registrations start immediately after it. Sizes the
    /// Patients module's two UHID sequences once, at provisioning time — see
    /// TenantProvisioningService. Defaults to 40,000.</summary>
    public int ImportedPatientCapacity { get; init; } = 40000;
}

public record CreateHospitalResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int ImportedPatientCapacity { get; init; }
}

public record TenantListItemResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string SubscriptionTier { get; init; } = string.Empty;
    public int ImportedPatientCapacity { get; init; }
}

public record TenantListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}

public record UpdateTenantStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// A soft-deleted hospital — <see cref="TenantListItemResponse"/> plus when it was deleted.
/// Only ever returned by the deleted-hospitals list; an active hospital's response never
/// carries this field since it's meaningless there.
/// </summary>
public record DeletedTenantListItemResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime DeletedAt { get; init; }
}

/// <summary>
/// Dry-run preview shown before a soft-delete is confirmed — deliberately Platform-side
/// data only (no row counts from the tenant's own database). See ADR for this finding: a
/// soft-delete never touches the tenant's own database at all (it stays fully intact,
/// login is just blocked), so there is nothing there to preview or warn about.
/// </summary>
public record TenantDeletePreviewResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>The Platform-level per-tenant configuration: which business-domain modules this
/// hospital's staff can use, and its subscription tier. See Tenant.EnabledModules's own doc
/// comment for how this is enforced.</summary>
public record TenantConfigurationResponse
{
    public Guid Id { get; init; }
    public IReadOnlyList<string> EnabledModules { get; init; } = [];
    public string SubscriptionTier { get; init; } = string.Empty;

    /// <summary>The full module catalog (ModuleCatalog.All), so the frontend can render a
    /// checkbox per module without hardcoding its own copy of the list.</summary>
    public IReadOnlyList<string> AllModules { get; init; } = [];
}

public record UpdateTenantConfigurationRequest
{
    public IReadOnlyList<string> EnabledModules { get; init; } = [];
    public string SubscriptionTier { get; init; } = string.Empty;
}

public record TenantDashboardStatsResponse
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Inactive { get; init; }

    /// <summary>Count of ProvisioningAlert rows — a tenant-provisioning failure whose
    /// rollback also failed, so an orphaned database may still exist. Zero in the normal
    /// case; any nonzero value needs a human to investigate (see ADR for this finding).</summary>
    public int ProvisioningAlertCount { get; init; }
}
