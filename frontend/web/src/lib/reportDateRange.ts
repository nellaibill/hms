import { addDaysToDate, hospitalDateOf, hospitalDayEnd, hospitalDayStart, hospitalToday } from './hospitalTime';

/** An instant as a `<input type="date">` value (YYYY-MM-DD): its calendar day in India
 * (ADR-147), whatever the PC's own time zone. Not `toISOString().slice(0, 10)` — that's the UTC
 * day, which is still yesterday until 05:30 every morning. Empty for an invalid date. */
export function toLocalDateInputValue(date: Date): string {
  return hospitalDateOf(date);
}

/** Today's date in India, by the server's clock, as a date-input value. */
export function todayDateInputValue(): string {
  return hospitalToday();
}

/** The default From/To for every report and date-filtered list: From = yesterday, To = today
 * (hospital dates) — one shared default instead of each page picking its own window. */
export function defaultReportDateRange(): { from: string; to: string } {
  const today = hospitalToday();
  return { from: addDaysToDate(today, -1), to: today };
}

/** A date-input value (YYYY-MM-DD) the way every report shows a date: "06 Nov 2026". Empty for
 * a missing or invalid value. */
export function formatReportDate(date: string | undefined): string {
  return formatDateInputValue(date);
}

/** A timestamp (ISO) as its local calendar day in report format, e.g. "06 Nov 2026". */
export function formatReportDateFromIso(iso: string | undefined): string {
  if (!iso) return '';
  const parsed = new Date(iso);
  return Number.isNaN(parsed.getTime()) ? '' : formatDateInputValue(toLocalDateInputValue(parsed));
}

function formatDateInputValue(date: string | undefined): string {
  // Midnight in India, shown in India's zone: the same calendar day on any PC (ADR-147).
  const parsed = hospitalDayStart(date);
  if (!parsed) return '';
  return parsed.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Kolkata' });
}

/** A From/To filter pair as a heading label, e.g. "01 Oct 2026 – 02 Oct 2026", or just
 * "02 Oct 2026" when both are the same day. Empty when neither date is set (or valid). */
export function formatDateRangeLabel(from: string | undefined, to: string | undefined): string {
  const fromLabel = formatDateInputValue(from);
  const toLabel = formatDateInputValue(to);
  if (fromLabel && toLabel) return fromLabel === toLabel ? fromLabel : `${fromLabel} – ${toLabel}`;
  if (fromLabel) return `From ${fromLabel}`;
  if (toLabel) return `Up to ${toLabel}`;
  return '';
}

// A plain `type="date"` value (e.g. "2026-09-11") has no time component — sending it to the
// backend as-is binds to midnight for BOTH From and To, so a same-day range like
// from=2026-09-11&to=2026-09-11 becomes a zero-width window (>= midnight AND <= midnight) that
// matches nothing created later that day. Widen to full-day bounds the same way
// StockLedgerPage's date filters already do (frontend/web/src/pages/pharmacy/StockLedgerPage.tsx)
// before sending either bound to the backend (OPD endpoints, Lab Worklist).
// A native <input type="date"> can fire onChange with an empty or transiently incomplete
// string while the user is still editing it (browser-dependent) — Date's constructor doesn't
// throw on a bad string, but the resulting "Invalid Date" DOES throw a RangeError from
// .toISOString(), and since this runs synchronously during render (every OPD tab's query hook
// calls these on every keystroke), an unguarded throw here crashes the whole page's render
// tree — confirmed live ("useAuth must be used within an AuthProvider" was React's own
// misleading secondary error from recovering a concurrent render, not the real cause). Guard
// explicitly rather than relying on the ternary's truthiness check alone.
// Both bounds are the day's start and end in India (ADR-147), not in the PC's own zone.
export const toRangeStart = (date: string | undefined): string | undefined => hospitalDayStart(date)?.toISOString();

export const toRangeEnd = (date: string | undefined): string | undefined => hospitalDayEnd(date)?.toISOString();
