import type { BillingType } from '../../enums';

/** Mirrors HMS.Shared.Kernel.MonthlyTotal — one calendar month's count or amount, in hospital
 * local time (IST). Returned oldest month first, zero-filled. */
export interface MonthlyTotal {
  year: number;
  month: number;
  value: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.RevenueByBillingTypeResponse. */
export interface RevenueByBillingType {
  billingType: BillingType;
  amount: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.BillingDashboardSummaryResponse — billed revenue only;
 * there is no expense ledger yet. */
export interface BillingDashboardSummary {
  monthlyRevenue: MonthlyTotal[];
  currentMonthByType: RevenueByBillingType[];
}
