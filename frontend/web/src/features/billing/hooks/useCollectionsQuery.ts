import { useQuery } from '@tanstack/react-query';
import { getCollections } from '../apiBillingRepository';
import { addDaysToDate, hospitalDayStart } from '@/lib/hospitalTime';

/** Turns a From–To pair of hospital calendar dates (YYYY-MM-DD) into the instants the API
 * takes: midnight IST at the start of `from` up to midnight IST after `to` (ADR-147), so a
 * payment taken at 00:30 IST lands on that IST day whatever the PC's own time zone. */
export function localDateRangeToInstants(from: string, to: string): { from: string; to: string } {
  const start = hospitalDayStart(from);
  const end = hospitalDayStart(addDaysToDate(to, 1));
  return { from: start?.toISOString() ?? '', to: end?.toISOString() ?? '' };
}

/** Money received between two hospital (IST) dates, inclusive (ADR-125) — the Collection Summary. */
export function useCollectionsQuery(range: { from: string; to: string }) {
  return useQuery({
    queryKey: ['billings', 'collections', range.from, range.to],
    queryFn: () => {
      const instants = localDateRangeToInstants(range.from, range.to);
      return getCollections(instants.from, instants.to);
    },
    enabled: Boolean(range.from && range.to && range.from <= range.to),
  });
}
