import { useQuery } from '@tanstack/react-query';
import { getCollections } from '../apiBillingRepository';

/** Turns a From–To pair of local calendar dates (YYYY-MM-DD) into the instants the API takes:
 * local midnight at the start of `from` up to local midnight after `to`. The browser's own
 * time zone is the hospital's, so a payment taken at 00:30 IST lands on that IST day. */
export function localDateRangeToInstants(from: string, to: string): { from: string; to: string } {
  const start = new Date(`${from}T00:00:00`);
  const end = new Date(`${to}T00:00:00`);
  end.setDate(end.getDate() + 1);
  return { from: start.toISOString(), to: end.toISOString() };
}

/** Money received between two local dates, inclusive (ADR-125) — the Collection Summary. */
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
