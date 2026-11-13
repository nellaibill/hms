import { NavLink } from 'react-router-dom';
import { usePendingDiscountCountQuery, usePendingRevertCountQuery } from '@/features/billing';
import { cn } from '@/lib/utils';
import { accountsPath } from '../accountsCategories';
import { useAccountsReportVisibility } from '../useAccountsReportVisibility';
import { useAccountsCategory } from '../useAccountsCategory';

/** The tab bar of one Accounts and Finance category (ADR-134) — OP, IP, Pharmacy or Lab, read
 * from the route the page sits under. Each tab is its own route, so any of them can be linked
 * or bookmarked directly. A report tab shows only once Settings → Reports Visibility turns it on
 * (ADR-120). `end` on NavLink so the landing tab doesn't stay highlighted on the tabs below it
 * (they share its path prefix); `flex-wrap` so a long bar doesn't clip on narrow widths. */
export function AccountsNavTabs() {
  const category = useAccountsCategory();
  const { isVisible } = useAccountsReportVisibility();
  const tabs = category.tabs.filter((tab) => !tab.report || isVisible(tab.report));
  const showsRevert = tabs.some((tab) => tab.pendingCount === 'revert');
  const showsDiscount = tabs.some((tab) => tab.pendingCount === 'discount');
  const { data: pendingRevertCount = 0 } = usePendingRevertCountQuery(showsRevert);
  const { data: pendingDiscountCount = 0 } = usePendingDiscountCountQuery(showsDiscount);
  const pendingCounts = { revert: pendingRevertCount, discount: pendingDiscountCount };

  return (
    <div className="flex flex-wrap items-center gap-1 overflow-x-auto border-b border-border">
      {tabs.map((tab) => {
        const pending = tab.pendingCount ? pendingCounts[tab.pendingCount] : 0;
        return (
          <NavLink
            key={tab.id}
            to={accountsPath(category.key, tab.path)}
            end
            className={({ isActive }) =>
              cn(
                'inline-flex shrink-0 items-center whitespace-nowrap rounded-t-md border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
                // Same tokens TabsTrigger (components/ui/tabs.tsx) and SidebarNav's active nav
                // item use — both admin-configurable via Theme & Branding — so this bar's
                // active-tab color always matches the rest of the app's tabs.
                isActive
                  ? 'border-primary bg-sidebar-active text-sidebar-active-foreground'
                  : 'border-transparent text-muted-foreground hover:bg-accent/50 hover:text-foreground',
              )
            }
          >
            {tab.label}
            {pending > 0 && (
              <span className="ml-1.5 rounded-full bg-warning px-1.5 text-xs font-semibold text-warning-foreground" aria-label={`${pending} pending`}>
                {pending}
              </span>
            )}
          </NavLink>
        );
      })}
    </div>
  );
}
