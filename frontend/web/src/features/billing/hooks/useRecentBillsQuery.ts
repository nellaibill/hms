import { useQuery } from '@tanstack/react-query';
import { getRecentBills } from '../apiBillingRepository';

export const recentBillsQueryKey = (count: number) => ['billings', 'recent', count] as const;

/** The latest `count` bills across every patient — see apiBillingRepository.getRecentBills. */
export function useRecentBillsQuery(count = 10) {
  return useQuery({
    queryKey: recentBillsQueryKey(count),
    queryFn: () => getRecentBills(count),
  });
}
