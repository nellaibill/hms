import { useQuery } from '@tanstack/react-query';
import type { AccountCategory } from '@hms/shared';
import { getRecentBills } from '../apiBillingRepository';

export const recentBillsQueryKey = (count: number, category?: AccountCategory) => ['billings', 'recent', count, category ?? 'all'] as const;

/** The latest `count` bills across every patient — only `category`'s when given (ADR-134). See
 * apiBillingRepository.getRecentBills. */
export function useRecentBillsQuery(count = 10, category?: AccountCategory) {
  return useQuery({
    queryKey: recentBillsQueryKey(count, category),
    queryFn: () => getRecentBills(count, category),
  });
}
