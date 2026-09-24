import type { MonthlyTotal } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { billingApi, ipdDashboardApi, patientsApi } from '@/services/apiClient';

export const DASHBOARD_MONTHS = 6;

/** "Apr", or "Apr 26" when the series spans two calendar years. */
export function monthLabel(entry: MonthlyTotal, spansYears: boolean) {
  const label = new Date(entry.year, entry.month - 1, 1).toLocaleDateString('en-IN', { month: 'short' });
  return spansYears ? `${label} ${String(entry.year).slice(-2)}` : label;
}

export function spansYears(series: MonthlyTotal[]) {
  return new Set(series.map((m) => m.year)).size > 1;
}

/** OP visits per month — patient-management.view (DASH-01: replaces the hard-coded census). */
export function useMonthlyOpVisitsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['dashboard', 'op-visits', DASHBOARD_MONTHS],
    queryFn: () => patientsApi.getMonthlyVisitCounts('OP', DASHBOARD_MONTHS),
    enabled,
    staleTime: 5 * 60_000,
  });
}

/** IP admissions per month — clinical-care.view + the IPD module. */
export function useMonthlyAdmissionsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['dashboard', 'ip-admissions', DASHBOARD_MONTHS],
    queryFn: () => ipdDashboardApi.getMonthlyAdmissions(DASHBOARD_MONTHS),
    enabled,
    staleTime: 5 * 60_000,
  });
}

/** Billed revenue per month + this month's revenue by billing type — finance-billing.view. */
export function useBillingDashboardQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['dashboard', 'billing-summary', DASHBOARD_MONTHS],
    queryFn: () => billingApi.getDashboardSummary(DASHBOARD_MONTHS),
    enabled,
    staleTime: 5 * 60_000,
  });
}
