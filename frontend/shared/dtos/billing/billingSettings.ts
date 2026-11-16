/**
 * Settings → Billing Approvals (ADR-136). Mirrors HMS.Modules.Billing.Contracts.BillingApprovalSettings
 * — the GET/PUT body of /api/v1/billing/settings. The defaults are how billing worked before.
 */
export interface BillingApprovalSettings {
  /** True hides "Add discount" and the API refuses any new discount. */
  discountsDisabled: boolean;
  /** True: a discount waits for its approvers (the consultant for their fee, the Super Admin for
   * the rest). False: it applies at once, recorded as approved by whoever added it. A Super
   * Admin's own discount always applies at once. */
  discountsNeedApproval: boolean;
  /** True: a bill revert waits for a Super Admin. False: the bill is reverted as soon as it's
   * requested, recorded as approved by whoever asked. */
  revertsNeedApproval: boolean;
}

export const DEFAULT_BILLING_APPROVAL_SETTINGS: BillingApprovalSettings = {
  discountsDisabled: false,
  discountsNeedApproval: true,
  revertsNeedApproval: true,
};
