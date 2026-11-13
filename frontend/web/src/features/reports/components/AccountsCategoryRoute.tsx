import { Outlet } from 'react-router-dom';
import { getAccountsCategory, type AccountsCategoryKey } from '../accountsCategories';
import { AccountsCategoryContext } from '../useAccountsCategory';

/**
 * Wraps every page of one Accounts and Finance category (ADR-134), so a page shared between
 * categories (Recent Bills, All Invoices, Collection Summary) knows which one it is showing and
 * the tab bar shows that category's tabs — read with useAccountsCategory. Outlet-based, like
 * RequireFeatureRoute.
 */
export function AccountsCategoryRoute({ category }: { category: AccountsCategoryKey }) {
  return (
    <AccountsCategoryContext.Provider value={getAccountsCategory(category)}>
      <Outlet />
    </AccountsCategoryContext.Provider>
  );
}
