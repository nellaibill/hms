import { getOverallPaymentStatus, type Billing } from '@/features/billing';
import { MOCK_EXPENSES } from './mockExpenses';
import type { ExpenseReportRow, IncomeReportRow, ReportDateRange } from './types';

function dateOnly(iso: string): string {
  return iso.slice(0, 10);
}

function inRange(date: string, range: ReportDateRange): boolean {
  return date >= range.from && date <= range.to;
}

function billingToIncomeRow(billing: Billing): IncomeReportRow {
  const billingTypes = Array.from(new Set(billing.items.map((item) => item.billingType))).join(', ');
  return {
    id: billing.id,
    invoiceNumber: billing.invoiceNumber,
    date: dateOnly(billing.createdAt),
    patientName: billing.patientName,
    patientUhid: billing.patientUhid,
    billingTypes,
    amount: billing.netAmount,
    paymentStatus: getOverallPaymentStatus(billing.items),
  };
}

/** One row per invoice (not per line item) — a financial report reads at ledger granularity, matching Total Income against the Unified Invoice Ledger's own totals. `billings` comes from features/billing's useInvoicesForReportQuery — this stays a pure transform so the page controls loading state. */
export function getIncomeRows(billings: Billing[], range: ReportDateRange): IncomeReportRow[] {
  return billings
    .map(billingToIncomeRow)
    .filter((row) => inRange(row.date, range))
    .sort((a, b) => b.date.localeCompare(a.date));
}

export function getExpenseRows(range: ReportDateRange): ExpenseReportRow[] {
  return MOCK_EXPENSES.filter((row) => inRange(row.date, range)).sort((a, b) => b.date.localeCompare(a.date));
}

export interface ReportTotals {
  totalIncome: number;
  totalExpense: number;
  net: number;
  /** Sum of income rows still awaiting payment — a real outstanding-receivables figure the
   * existing three totals don't surface on their own (Total Income already includes Pending
   * invoices, so this isn't derivable from the other cards at a glance). */
  pendingAmount: number;
}

export function getReportTotals(income: IncomeReportRow[], expense: ExpenseReportRow[]): ReportTotals {
  const totalIncome = income.reduce((sum, row) => sum + row.amount, 0);
  const totalExpense = expense.reduce((sum, row) => sum + row.amount, 0);
  const pendingAmount = income.filter((row) => row.paymentStatus === 'Pending').reduce((sum, row) => sum + row.amount, 0);
  return { totalIncome, totalExpense, net: totalIncome - totalExpense, pendingAmount };
}

export interface BreakdownRow {
  label: string;
  amount: number;
}

/**
 * Income grouped by billing type (Consultation/Radiology/Laboratory/Procedure/Pharmacy) — reads
 * straight from each invoice's line items (`billing.items[].total`/`.billingType`) rather than
 * `IncomeReportRow.billingTypes` (a comma-joined string of the *distinct* types on an invoice,
 * which can't say how much of a mixed-type invoice's total belongs to each type). Filtered by
 * the invoice's own date, same as getIncomeRows, so this always matches what's on screen.
 */
export function getIncomeByBillingType(billings: Billing[], range: ReportDateRange): BreakdownRow[] {
  const totals = new Map<string, number>();
  for (const billing of billings) {
    if (!inRange(dateOnly(billing.createdAt), range)) continue;
    for (const item of billing.items) {
      totals.set(item.billingType, (totals.get(item.billingType) ?? 0) + item.total);
    }
  }
  return Array.from(totals, ([label, amount]) => ({ label, amount })).sort((a, b) => b.amount - a.amount);
}

/** Expenses grouped by category — same shape as getIncomeByBillingType, over the already
 * date-filtered expense rows (see getExpenseRows) rather than re-filtering here. */
export function getExpensesByCategory(expenseRows: ExpenseReportRow[]): BreakdownRow[] {
  const totals = new Map<string, number>();
  for (const row of expenseRows) {
    totals.set(row.category, (totals.get(row.category) ?? 0) + row.amount);
  }
  return Array.from(totals, ([label, amount]) => ({ label, amount })).sort((a, b) => b.amount - a.amount);
}
