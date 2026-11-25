import { hospitalDateOf } from '@/lib/hospitalTime';

/** Date key (yyyy-mm-dd) of an ISO timestamp — its day in India (ADR-147) — comparable with an
 * <input type="date"> value. */
function localDateKey(iso: string): string {
  return hospitalDateOf(iso);
}

/** Inclusive, by the hospital's (IST) calendar day. An empty bound is open-ended. */
export function inDateRange(iso: string, from: string, to: string): boolean {
  if (!from && !to) return true;
  const key = localDateKey(iso);
  return (!from || key >= from) && (!to || key <= to);
}
