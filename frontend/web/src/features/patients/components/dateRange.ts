/** Local-date key (yyyy-mm-dd) of an ISO timestamp, comparable with an <input type="date"> value. */
function localDateKey(iso: string): string {
  const date = new Date(iso);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Inclusive, by the viewer's local calendar day. An empty bound is open-ended. */
export function inDateRange(iso: string, from: string, to: string): boolean {
  if (!from && !to) return true;
  const key = localDateKey(iso);
  return (!from || key >= from) && (!to || key <= to);
}
