import { DEFAULT_BILLING_APPROVAL_SETTINGS, type BillingApprovalSettings } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/features/auth/AuthContext';
import { billingApi } from '@/services/apiClient';

/** Under 'billings' like the other billing keys, so it refreshes with them. */
export const billingSettingsQueryKey = ['billings', 'settings'] as const;

/** Settings → Billing Approvals (ADR-136), as saved. */
export function useBillingSettingsQuery() {
  return useQuery({
    queryKey: billingSettingsQueryKey,
    queryFn: () => billingApi.getBillingSettings(),
    // A Super Admin can change these at any time; the billing screens read them fresh.
    staleTime: 0,
  });
}

export interface BillingApprovalRules extends BillingApprovalSettings {
  /** A discount added by the current user applies at once: they're the Super Admin, or
   * discount approval is switched off. */
  discountAppliesAtOnce: boolean;
}

/**
 * For the billing screens: the hospital's settings, or the defaults (discounts allowed, approvals
 * on) while they load or if they can't be loaded. The API enforces the real settings either way —
 * this only decides what the screens show.
 */
export function useBillingApprovalRules(): BillingApprovalRules {
  const { user } = useAuth();
  const { data } = useBillingSettingsQuery();
  const settings = data ?? DEFAULT_BILLING_APPROVAL_SETTINGS;
  return {
    ...settings,
    discountAppliesAtOnce: user?.role === 'superAdmin' || !settings.discountsNeedApproval,
  };
}

export function useUpdateBillingSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: BillingApprovalSettings) => billingApi.updateBillingSettings(request),
    onSuccess: (saved) => queryClient.setQueryData(billingSettingsQueryKey, saved),
  });
}
