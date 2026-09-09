import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';

const TABS = [
  { to: '/finance/accounts', label: 'Recent Bills' },
  { to: '/finance/accounts/invoices', label: 'All Invoices' },
  { to: '/finance/accounts/reports', label: 'Income & Expense' },
  { to: '/finance/accounts/reports/profit', label: 'Hospital Profit' },
  { to: '/finance/accounts/reports/laboratory', label: 'Laboratory' },
  { to: '/finance/accounts/reports/radiology', label: 'Radiology' },
  { to: '/finance/accounts/reports/consultant', label: 'Consultant' },
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
  return (
    <div className="flex flex-wrap items-center gap-1 overflow-x-auto border-b border-border">
      {TABS.map((tab) => (
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
        </NavLink>
      ))}
    </div>
  );
}
