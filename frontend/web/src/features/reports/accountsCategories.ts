import type { AccountCategory } from '@hms/shared';
import { BedDouble, FlaskConical, Pill, Stethoscope, type LucideIcon } from 'lucide-react';
import type { AccountsReport } from '@/features/branding/types';

/**
 * Accounts and Finance is split into categories (ADR-134): its landing page is a hub of one
 * card per category, and each category has its own tab bar. This file is the single source for
 * the cards, the tab bars and the routes (routes.tsx builds one route per tab from it).
 */
export type AccountsCategoryKey = 'op' | 'ip' | 'pharmacy' | 'lab';

/** Every page a tab can open — routes.tsx maps each one to its page component. */
export type AccountsTabId =
  | 'recentBills'
  | 'allInvoices'
  | 'revertRequests'
  | 'discountRequests'
  | 'incomeExpense'
  | 'opReport'
  | 'collectionSummary'
  | 'consultant'
  | 'pharmacyBills'
  | 'pharmacyReport'
  | 'laboratory'
  | 'radiology';

export interface AccountsTab {
  id: AccountsTabId;
  /** Path under the category's own base path; '' is the category's landing tab. */
  path: string;
  label: string;
  /** Shown only once Settings → Reports Visibility turns it on (ADR-120). */
  report?: AccountsReport;
  /** Shows the pending approvals count on the tab. */
  pendingCount?: 'revert' | 'discount';
}

export interface AccountsCategoryConfig {
  key: AccountsCategoryKey;
  /** Card and banner title, e.g. "OP Accounts". */
  label: string;
  /** Short name used in headings, e.g. "Recent OP Bills". */
  shortLabel: string;
  description: string;
  icon: LucideIcon;
  /** Tenant module the category needs; the card and its pages are hidden without it. */
  feature?: string;
  /** The bill-list filter for this category. Pharmacy has none: its bills aren't invoices (ADR-094). */
  invoiceCategory?: AccountCategory;
  /** Whether its bill lists offer New Invoice. IP bills come from the discharge final bill. */
  canCreateInvoice: boolean;
  /** Encounter types its Collection Summary counts (ADR-125 keeps OP's to OP visits only). */
  collectionEncounterTypes?: readonly string[];
  tabs: AccountsTab[];
}

export const ACCOUNTS_HUB_PATH = '/finance/accounts';

/** The encounter types listed under IP Accounts — the ones that get an IP No (ADR-104). Mirrors
 * HMS.Modules.Billing.Domain.AccountCategoryRules. */
export const IP_ENCOUNTER_TYPES = ['IP', 'DayCare', 'Observation'] as const;

export const ACCOUNTS_CATEGORIES: readonly AccountsCategoryConfig[] = [
  {
    key: 'op',
    label: 'OP Accounts',
    shortLabel: 'OP',
    description: 'Outpatient and emergency bills, approvals, OP Report, collections and consultant shares.',
    icon: Stethoscope,
    invoiceCategory: 'Op',
    canCreateInvoice: true,
    collectionEncounterTypes: ['OP'],
    tabs: [
      { id: 'recentBills', path: '', label: 'Recent Bills' },
      { id: 'allInvoices', path: 'invoices', label: 'All Invoices' },
      // OP bill revert requests (ADR-106) — the Super Admin's approval queue.
      { id: 'revertRequests', path: 'revert-requests', label: 'Revert Requests', pendingCount: 'revert' },
      // Bill discount requests (ADR-109) — only OP charges can be discounted.
      { id: 'discountRequests', path: 'discount-requests', label: 'Discount Requests', pendingCount: 'discount' },
      { id: 'incomeExpense', path: 'reports', label: 'Income & Expense', report: 'incomeExpense' },
      { id: 'opReport', path: 'reports/profit', label: 'OP Report' },
      // Money received by payment date (ADR-125) — always shows, like OP Report.
      { id: 'collectionSummary', path: 'reports/collection', label: 'Collection Summary' },
      { id: 'consultant', path: 'reports/consultant', label: 'Consultant', report: 'consultant' },
    ],
  },
  {
    key: 'ip',
    label: 'IP Accounts',
    shortLabel: 'IP',
    description: 'Inpatient, day care and observation bills, including discharge final bills, and their collections.',
    icon: BedDouble,
    feature: 'ipd',
    invoiceCategory: 'Ip',
    canCreateInvoice: false,
    collectionEncounterTypes: IP_ENCOUNTER_TYPES,
    tabs: [
      { id: 'recentBills', path: '', label: 'Recent Bills' },
      { id: 'allInvoices', path: 'invoices', label: 'All Invoices' },
      { id: 'collectionSummary', path: 'reports/collection', label: 'Collection Summary' },
    ],
  },
  {
    key: 'pharmacy',
    label: 'Pharmacy Accounts',
    shortLabel: 'Pharmacy',
    description: 'Pharmacy counter bills, balances due and returns, with day-wise sales, GST and margin.',
    icon: Pill,
    feature: 'pharmacy',
    canCreateInvoice: false,
    tabs: [
      // Pharmacy bills are the pharmacy's own documents (ADR-094), never hospital invoices.
      { id: 'pharmacyBills', path: '', label: 'Pharmacy Bills' },
      { id: 'pharmacyReport', path: 'reports', label: 'Pharmacy Report', report: 'pharmacy' },
    ],
  },
  {
    key: 'lab',
    label: 'Lab Accounts',
    shortLabel: 'Lab',
    description: 'Every bill with a lab test, with lab and radiology revenue, cost and profit.',
    icon: FlaskConical,
    invoiceCategory: 'Lab',
    canCreateInvoice: true,
    tabs: [
      { id: 'recentBills', path: '', label: 'Recent Bills' },
      { id: 'allInvoices', path: 'invoices', label: 'All Invoices' },
      { id: 'laboratory', path: 'reports/laboratory', label: 'Laboratory', report: 'laboratory' },
      { id: 'radiology', path: 'reports/radiology', label: 'Radiology', report: 'radiology' },
    ],
  },
];

export function getAccountsCategory(key: AccountsCategoryKey): AccountsCategoryConfig {
  return ACCOUNTS_CATEGORIES.find((category) => category.key === key)!;
}

/** The URL of one of a category's tabs, or of the category itself when `tabPath` is ''. */
export function accountsPath(key: AccountsCategoryKey, tabPath = ''): string {
  return tabPath ? `${ACCOUNTS_HUB_PATH}/${key}/${tabPath}` : `${ACCOUNTS_HUB_PATH}/${key}`;
}

/** Where each Accounts URL from before the categories now lives, so bookmarks and old links
 * keep working. */
export const LEGACY_ACCOUNTS_REDIRECTS: readonly (readonly [from: string, to: string])[] = [
  ['/finance/accounts/invoices', accountsPath('op', 'invoices')],
  ['/finance/accounts/revert-requests', accountsPath('op', 'revert-requests')],
  ['/finance/accounts/discount-requests', accountsPath('op', 'discount-requests')],
  ['/finance/accounts/reports', accountsPath('op', 'reports')],
  ['/finance/accounts/reports/profit', accountsPath('op', 'reports/profit')],
  ['/finance/accounts/reports/collection', accountsPath('op', 'reports/collection')],
  ['/finance/accounts/reports/consultant', accountsPath('op', 'reports/consultant')],
  ['/finance/accounts/reports/laboratory', accountsPath('lab', 'reports/laboratory')],
  ['/finance/accounts/reports/radiology', accountsPath('lab', 'reports/radiology')],
  ['/finance/accounts/reports/pharmacy', accountsPath('pharmacy', 'reports')],
];
