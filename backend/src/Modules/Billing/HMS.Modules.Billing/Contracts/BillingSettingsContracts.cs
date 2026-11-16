namespace HMS.Modules.Billing.Contracts;

/// <summary>
/// Settings → Billing Approvals (ADR-136): how the hospital handles bill discounts and bill
/// reverts. Also the GET/PUT body of <c>api/v1/billing/settings</c>. The defaults are how
/// billing worked before the page existed. Kept in sync with
/// frontend/shared/dtos/billing/billingSettings.ts.
/// </summary>
public record BillingApprovalSettings
{
    /// <summary>True hides "Add discount" and makes the API refuse any new discount, on a new
    /// bill or a saved one. Discounts already given are untouched.</summary>
    public bool DiscountsDisabled { get; init; }

    /// <summary>True: a discount waits for its approvers (ADR-111: the consultant for their
    /// consultation fee, the Super Admin for the rest), and the bill can't be paid until then.
    /// False: every discount applies at once, recorded as approved by the person who added it.
    /// A Super Admin's own discount always applies at once.</summary>
    public bool DiscountsNeedApproval { get; init; } = true;

    /// <summary>True: a bill revert request waits for a Super Admin (ADR-106). False: the bill is
    /// reverted as soon as it's requested, recorded as approved by the person who asked. The
    /// eligibility rules (OP bills only, same day) apply either way.</summary>
    public bool RevertsNeedApproval { get; init; } = true;

    /// <summary>A hospital that hasn't saved the page yet.</summary>
    public static BillingApprovalSettings Default { get; } = new();
}
