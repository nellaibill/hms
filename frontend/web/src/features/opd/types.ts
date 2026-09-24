import { defaultReportDateRange, todayDateInputValue } from '@/lib/reportDateRange';

export type OpdTab = 'patients' | 'consultations' | 'investigations' | 'procedures' | 'admissions';

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
