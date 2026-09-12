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

// A plain `type="date"` value (e.g. "2026-09-11") has no time component — sending it to the
// backend as-is binds to midnight for BOTH From and To, so a same-day range like
// from=2026-09-11&to=2026-09-11 becomes a zero-width window (>= midnight AND <= midnight) that
// matches nothing created later that day. Widen to full-day bounds the same way
// StockLedgerPage's date filters already do (frontend/web/src/pages/pharmacy/StockLedgerPage.tsx)
// before sending either bound to any OPD endpoint.
export const toRangeStart = (date: string | undefined): string | undefined =>
  date ? new Date(`${date}T00:00:00`).toISOString() : undefined;

export const toRangeEnd = (date: string | undefined): string | undefined =>
  date ? new Date(`${date}T23:59:59.999`).toISOString() : undefined;

export const emptyOpdFilters = (): OpdFilterValues => ({
  from: todayIsoDate(),
  to: todayIsoDate(),
  departmentId: undefined,
  consultantId: undefined,
  status: undefined,
  search: '',
});
