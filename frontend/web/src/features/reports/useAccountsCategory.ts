import { createContext, useContext } from 'react';
import { getAccountsCategory, type AccountsCategoryConfig } from './accountsCategories';

/** Set by AccountsCategoryRoute for every page of one Accounts category (ADR-134). */
export const AccountsCategoryContext = createContext<AccountsCategoryConfig | null>(null);

/** The category the current Accounts page belongs to; OP outside any category route (a page
 * rendered on its own). */
export function useAccountsCategory(): AccountsCategoryConfig {
  return useContext(AccountsCategoryContext) ?? getAccountsCategory('op');
}
