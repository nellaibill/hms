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

export const todayIsoDate = () => new Date().toISOString().slice(0, 10);

export const emptyOpdFilters = (): OpdFilterValues => ({
  from: todayIsoDate(),
  to: todayIsoDate(),
  departmentId: undefined,
  consultantId: undefined,
  status: undefined,
  search: '',
});
