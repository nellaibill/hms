import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';

const TABS = [
  { to: '/finance/accounts/reports', label: 'Income & Expense' },
  { to: '/finance/accounts/reports/profit', label: 'Hospital Profit' },
  { to: '/finance/accounts/reports/laboratory', label: 'Laboratory' },
  { to: '/finance/accounts/reports/radiology', label: 'Radiology' },
  { to: '/finance/accounts/reports/consultant', label: 'Consultant' },
];

/** Switches between Finance & Billing's separate report pages — each is its own route (so
 * either can be linked/bookmarked directly), with this bar giving a way to move between them
 * without going back through the Ledger. `end` on NavLink so the Income & Expense tab doesn't
 * stay highlighted while viewing another report (every route shares the `/reports` prefix).
 * `flex-wrap` so five tabs (up from the original two) don't overflow/clip on narrow widths. */
export function ReportNavTabs() {
  return (
    <div className="flex flex-wrap gap-1 border-b border-border">
      {TABS.map((tab) => (
        <NavLink
          key={tab.to}
          to={tab.to}
          end
          className={({ isActive }) =>
            cn(
              'border-b-2 px-3 py-2 text-sm font-medium transition-colors',
              isActive ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
            )
          }
        >
          {tab.label}
        </NavLink>
      ))}
    </div>
  );
}
