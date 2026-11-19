import { useQuery } from '@tanstack/react-query';
import { getInvoicesSince } from '../apiBillingRepository';

/** Bills created since `fromIso` (newest first, fetched only as far back as needed) — for the
 * dashboard's "today" figures. Refreshes every minute while the dashboard is open. */
export function useInvoicesSinceQuery(fromIso: string) {
  return useQuery({
    queryKey: ['billings', 'since', fromIso],
    queryFn: () => getInvoicesSince(fromIso),
    refetchInterval: 60_000,
  });
}
