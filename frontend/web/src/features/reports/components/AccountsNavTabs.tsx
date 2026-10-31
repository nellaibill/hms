import { NavLink } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthContext';
import { usePendingDiscountCountQuery, usePendingRevertCountQuery } from '@/features/billing';
import { cn } from '@/lib/utils';

const TABS: { to: string; label: string; feature?: string }[] = [
  { to: '/finance/accounts', label: 'Recent Bills' },
  { to: '/finance/accounts/invoices', label: 'All Invoices' },
  // OP bill revert requests (ADR-106) — the Super Admin's approval queue.
  { to: '/finance/accounts/revert-requests', label: 'Revert Requests' },
  // Bill discount requests (ADR-109) — the Super Admin's other approval queue.
  { to: '/finance/accounts/discount-requests', label: 'Discount Requests' },
  { to: '/finance/accounts/reports', label: 'Income & Expense' },
  { to: '/finance/accounts/reports/profit', label: 'Hospital Profit' },
  { to: '/finance/accounts/reports/laboratory', label: 'Laboratory' },
  { to: '/finance/accounts/reports/radiology', label: 'Radiology' },
  { to: '/finance/accounts/reports/consultant', label: 'Consultant' },
  // Pharmacy bills are not hospital invoices (ADR-094); this tab is where finance reads them.
  { to: '/finance/accounts/reports/pharmacy', label: 'Pharmacy', feature: 'pharmacy' },
];

/** One shared tab bar across the whole Accounts & Finance section — the Ledger (Recent Bills,
 * All Invoices) and every report used to be two separate navigation systems (a Ledger-page-only
 * Tabs widget plus this bar, which only spanned the report pages) reached via a "Reports"
 * button; combined into one continuous bar per the user's own follow-up request, so every view
 * in the section is a single click away from any other, no separate button needed. Each tab is
 * still its own route (so any of them can be linked/bookmarked directly, not just the Ledger).
 * `end` on NavLink so "Recent Bills" doesn't stay highlighted while viewing a report (every
 * report route shares the `/finance/accounts` prefix). `flex-wrap` so seven tabs don't
 * overflow/clip on narrow widths. */
export function AccountsNavTabs() {
  const { hasFeature } = useAuth();
  const tabs = TABS.filter((tab) => !tab.feature || hasFeature(tab.feature));
  const { data: pendingRevertCount = 0 } = usePendingRevertCountQuery();
  const { data: pendingDiscountCount = 0 } = usePendingDiscountCountQuery();
  const pendingCountByTab: Record<string, number> = {
    '/finance/accounts/revert-requests': pendingRevertCount,
    '/finance/accounts/discount-requests': pendingDiscountCount,
  };

  return (
    <div className="flex flex-wrap items-center gap-1 overflow-x-auto border-b border-border">
      {tabs.map((tab) => (
        <NavLink
          key={tab.to}
          to={tab.to}
          end
          className={({ isActive }) =>
            cn(
              'inline-flex shrink-0 items-center whitespace-nowrap rounded-t-md border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
              // Same tokens TabsTrigger (components/ui/tabs.tsx) and SidebarNav's active nav
              // item use — both admin-configurable via Theme & Branding — rather than the
              // generic `accent`/`primary` pair alone, so this bar's active-tab color always
              // matches the rest of the app's tabs instead of drifting once a hospital sets a
              // custom brand color.
              isActive
                ? 'border-primary bg-sidebar-active text-sidebar-active-foreground'
                : 'border-transparent text-muted-foreground hover:bg-accent/50 hover:text-foreground',
            )
          }
        >
          {tab.label}
          {(pendingCountByTab[tab.to] ?? 0) > 0 && (
            <span
              className="ml-1.5 rounded-full bg-warning px-1.5 text-xs font-semibold text-warning-foreground"
              aria-label={`${pendingCountByTab[tab.to]} pending`}
            >
              {pendingCountByTab[tab.to]}
            </span>
          )}
        </NavLink>
      ))}
    </div>
  );
}
