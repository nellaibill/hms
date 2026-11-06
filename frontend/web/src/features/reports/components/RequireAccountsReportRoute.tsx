import { Loader2 } from 'lucide-react';
import { Navigate, Outlet } from 'react-router-dom';
import type { AccountsReport } from '@/features/branding/types';
import { ALWAYS_VISIBLE_REPORT_PATH, useAccountsReportVisibility } from '../useAccountsReportVisibility';

interface RequireAccountsReportRouteProps {
  report: AccountsReport;
}

/**
 * Route guard for an Accounts & Finance report the Reports visibility setting can hide
 * (ADR-120). Outlet-based, like RequireFeatureRoute. A hidden report's URL (an old bookmark,
 * a pasted link) goes to OP Report, which always shows, rather than to a "not available"
 * message, because the tab bar no longer offers that report either. The reports are built in
 * the browser from the invoice list, so this guard is the whole boundary: there is no
 * per-report endpoint to lock.
 */
export function RequireAccountsReportRoute({ report }: RequireAccountsReportRouteProps) {
  const { isLoading, isVisible } = useAccountsReportVisibility();

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading…
      </div>
    );
  }

  if (!isVisible(report)) {
    return <Navigate to={ALWAYS_VISIBLE_REPORT_PATH} replace />;
  }

  return <Outlet />;
}
