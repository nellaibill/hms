using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// One row per hospital database: how discounts and bill reverts are approved (ADR-136). Null
/// until a Super Admin first saves Settings → Billing Approvals — the defaults
/// (Contracts.BillingApprovalSettings.Default, i.e. approvals on) stand in until then. Stored as
/// one serialized Contracts.BillingApprovalSettings in a jsonb column, so a setting added later
/// needs no migration.
/// </summary>
internal class BillingSettings : Entity
{
    public string ApprovalSettingsJson { get; private set; } = null!;

    // Required by EF Core materialization.
    private BillingSettings()
    {
    }

    private BillingSettings(Guid id, string approvalSettingsJson, Guid? createdBy)
        : base(id, createdBy)
    {
        ApprovalSettingsJson = approvalSettingsJson;
    }

    public static BillingSettings Create(string approvalSettingsJson, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(approvalSettingsJson, nameof(approvalSettingsJson));
        return new BillingSettings(Guid.CreateVersion7(), approvalSettingsJson, createdBy);
    }

    public void Update(string approvalSettingsJson, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(approvalSettingsJson, nameof(approvalSettingsJson));
        ApprovalSettingsJson = approvalSettingsJson;
        MarkUpdated(updatedBy);
    }
}
