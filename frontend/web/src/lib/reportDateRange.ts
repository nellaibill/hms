/** A Date as a `<input type="date">` value (YYYY-MM-DD) in the user's *local* calendar day.
 * Not `toISOString().slice(0, 10)` — that's the UTC day, which in IST (UTC+5:30) is still
 * yesterday until 05:30 every morning, so "today" defaults silently came out a day early. */
export function toLocalDateInputValue(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Today's local date as a date-input value. */
export function todayDateInputValue(): string {
  return toLocalDateInputValue(new Date());
}

/** The default From/To for every report and date-filtered list: From = yesterday, To = today
 * (local dates) — one shared default instead of each page picking its own window. */
export function defaultReportDateRange(): { from: string; to: string } {
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  return { from: toLocalDateInputValue(yesterday), to: toLocalDateInputValue(today) };
}

function formatDateInputValue(date: string | undefined): string {
  if (!date) return '';
  // Parsed as a local date, not `new Date(date)` — a bare YYYY-MM-DD is read as UTC midnight,
  // which is the previous calendar day anywhere west of UTC.
  const parsed = new Date(`${date}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) return '';
  return parsed.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
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
