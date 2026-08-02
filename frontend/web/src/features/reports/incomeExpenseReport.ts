import { getAllMockBillings, getOverallPaymentStatus, type Billing } from '@/features/billing';
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
    date: dateOnly(billing.createdAt),
    patientName: billing.patientName,
    patientUhid: billing.patientUhid,
    billingTypes,
    amount: billing.netAmount,
    paymentStatus: getOverallPaymentStatus(billing.items),
  };
}

/** One row per invoice (not per line item) — a financial report reads at ledger granularity, matching Total Income against the Unified Invoice Ledger's own totals. */
export function getIncomeRows(range: ReportDateRange): IncomeReportRow[] {
  return getAllMockBillings()
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
}

export function getReportTotals(income: IncomeReportRow[], expense: ExpenseReportRow[]): ReportTotals {
  const totalIncome = income.reduce((sum, row) => sum + row.amount, 0);
  const totalExpense = expense.reduce((sum, row) => sum + row.amount, 0);
  return { totalIncome, totalExpense, net: totalIncome - totalExpense };
}
