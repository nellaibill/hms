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
