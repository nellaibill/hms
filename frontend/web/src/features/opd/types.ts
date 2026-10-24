import { defaultReportDateRange, todayDateInputValue } from '@/lib/reportDateRange';

export type OpdTab = 'patients' | 'consultations' | 'investigations' | 'procedures' | 'admissions' | 'scheduled' | 'marked';

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
  return new Date(year, month - 1, day).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
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
