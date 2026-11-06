import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import type { AccountsReport } from '@/features/branding/types';

/** Where a hidden report's URL sends the user: OP Report always shows (ADR-120). */
export const ALWAYS_VISIBLE_REPORT_PATH = '/finance/accounts/reports/profit';

/**
 * The hospital's Reports visibility setting (Settings → Reports Visibility, ADR-120), read
 * from the branding config every signed-in user already loads. Hidden for everyone, Super
 * Admins included, until an admin turns a report on. While branding is still loading every
 * report counts as hidden, so a tab never flashes up and then disappears.
 */
export function useAccountsReportVisibility() {
  const { data, isPending } = useBrandingQuery();
  const visible = new Set<AccountsReport>(data?.reportVisibility.visibleReports ?? []);
  return {
    isLoading: isPending,
    isVisible: (report: AccountsReport) => visible.has(report),
  };
}
