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

// A plain `type="date"` value (e.g. "2026-09-11") has no time component — sending it to the
// backend as-is binds to midnight for BOTH From and To, so a same-day range like
// from=2026-09-11&to=2026-09-11 becomes a zero-width window (>= midnight AND <= midnight) that
// matches nothing created later that day. Widen to full-day bounds the same way
// StockLedgerPage's date filters already do (frontend/web/src/pages/pharmacy/StockLedgerPage.tsx)
// before sending either bound to any OPD endpoint.
// A native <input type="date"> can fire onChange with an empty or transiently incomplete
// string while the user is still editing it (browser-dependent) — Date's constructor doesn't
// throw on a bad string, but the resulting "Invalid Date" DOES throw a RangeError from
// .toISOString(), and since this runs synchronously during render (every OPD tab's query hook
// calls these on every keystroke), an unguarded throw here crashes the whole page's render
// tree — confirmed live ("useAuth must be used within an AuthProvider" was React's own
// misleading secondary error from recovering a concurrent render, not the real cause). Guard
// explicitly rather than relying on the ternary's truthiness check alone.
export const toRangeStart = (date: string | undefined): string | undefined => {
  if (!date) return undefined;
  const parsed = new Date(`${date}T00:00:00`);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
};

export const toRangeEnd = (date: string | undefined): string | undefined => {
  if (!date) return undefined;
  const parsed = new Date(`${date}T23:59:59.999`);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
};

export const emptyOpdFilters = (): OpdFilterValues => ({
  ...defaultReportDateRange(),
  departmentId: undefined,
  consultantId: undefined,
  status: undefined,
  search: '',
});
