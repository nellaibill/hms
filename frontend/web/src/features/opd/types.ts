import { defaultReportDateRange, todayDateInputValue } from '@/lib/reportDateRange';

/** 'discounts' is a consultant login's own Discount Approvals tab (ADR-111) — it has no filters or export. */
export type OpdTab = 'patients' | 'consultations' | 'investigations' | 'procedures' | 'admissions' | 'scheduled' | 'marked' | 'discounts';

/** Shared filter values driven by OpdFilterBar and consumed by every tab's own query hook —
 * which fields actually apply (e.g. `status`'s option list) depends on the active tab. */
export interface OpdFilterValues {
  from: string;
  to: string;
  departmentId: string | undefined;
  consultantId: string | undefined;
  status: string | undefined;
  search: string;
}

/** Local calendar day, not the UTC one — see lib/reportDateRange. */
export const todayIsoDate = todayDateInputValue;

/** A Scheduled Visit's "YYYY-MM-DD" date as a local calendar day — new Date("YYYY-MM-DD") alone
 * is UTC midnight, which shows as the previous day west of UTC. */
export function formatScheduledDate(value: string): string {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' });
}

/** Scheduled Visits keeps its own From/To, separate from the yesterday–today range the other OPD
 * tabs share: it looks ahead, so it starts today with no To — every upcoming visit, as the tab
 * listed before it had date filters. */
export const defaultScheduledVisitsRange = (): { from: string; to: string } => ({ from: todayIsoDate(), to: '' });

/** The range Scheduled Visits actually covers: never before `today` (past review dates don't show
 * — the API starts a past From today too), and an empty To means no end. */
export function effectiveScheduledRange(from: string, to: string, today: string): { from: string; to: string | undefined } {
  return { from: from && from > today ? from : today, to: to || undefined };
}

/** Scheduled Visits within `from`–`to` ("YYYY-MM-DD" local days; no `to` = no end) — the API
 * already applies the range, but cached rows, or a page left open past midnight, can still hold
 * a day that has gone. "YYYY-MM-DD" strings compare in date order. */
export function scheduledVisitsInRange<T extends { scheduledDate: string }>(rows: readonly T[], from: string, to?: string): T[] {
  return rows.filter((row) => row.scheduledDate >= from && (!to || row.scheduledDate <= to));
}

/** When a patient was marked (an ISO timestamp) as local date and time. */
export function formatMarkedAt(value: string): string {
  return new Date(value).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true });
}

// Full-day bounds for a date-input value — moved to lib/reportDateRange (shared with the Lab
// Worklist); re-exported so every OPD tab's existing import keeps working.
export { toRangeEnd, toRangeStart } from '@/lib/reportDateRange';

export const emptyOpdFilters = (): OpdFilterValues => ({
  ...defaultReportDateRange(),
  departmentId: undefined,
  consultantId: undefined,
  status: undefined,
  search: '',
});
