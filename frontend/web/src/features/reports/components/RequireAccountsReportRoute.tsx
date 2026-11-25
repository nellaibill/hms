import { Loader2 } from 'lucide-react';
import { Navigate, Outlet } from 'react-router-dom';
import type { AccountsReport } from '@/features/branding/types';
import { accountsPath, visibleAccountsTabs } from '../accountsCategories';
import { useAccountsReportVisibility } from '../useAccountsReportVisibility';
import { useAccountsCategory } from '../useAccountsCategory';

interface RequireAccountsReportRouteProps {
  report: AccountsReport;
}

/**
 * Route guard for an Accounts & Finance report the Reports visibility setting can hide
 * (ADR-120). Outlet-based, like RequireFeatureRoute. A hidden report's URL (an old bookmark,
 * a pasted link) goes to its category's first tab that shows (ADR-134/151), rather than to a
 * "not available" message, because the tab bar no longer offers that report either. The
 * reports are built in the browser from the invoice list, so this guard is the whole boundary:
 * there is no per-report endpoint to lock.
 */
export function RequireAccountsReportRoute({ report }: RequireAccountsReportRouteProps) {
  const { isLoading, isVisible } = useAccountsReportVisibility();
  const category = useAccountsCategory();

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading…
      </div>
    );
  }

  if (!isVisible(report)) {
    // The category's first tab that shows — not its landing tab, which can be hidden too since
    // ADR-151 (Recent Bills), and sending a hidden landing tab to itself would loop.
    const firstShown = visibleAccountsTabs(category, isVisible)[0];
    if (!firstShown) {
      return (
        <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
          <p className="font-medium text-foreground">Nothing is switched on for {category.label}.</p>
          <p className="text-sm text-muted-foreground">A Super Admin can turn its tabs on in Settings → Reports Visibility.</p>
        </div>
      );
    }
    return <Navigate to={accountsPath(category.key, firstShown.path)} replace />;
  }

  return <Outlet />;
}
