import { useQuery } from '@tanstack/react-query';
import { listMockBillings, type BillingListQuery, type PagedBillings } from '../mockBillingStore';

export const billingsQueryKey = (query: BillingListQuery) => ['billings', 'list', query] as const;

/**
 * Wraps the mock store directly rather than going through an apiBillingRepository-style
 * fallback (see features/roles/apiRoleRepository.ts) — there's no real billingApi yet to
 * fall back *from*, so faking that layer would just be dead code. Swap this queryFn for a
 * real repository call once HMS.Modules.Billing exists.
 */
export function useBillingsQuery(query: BillingListQuery) {
  return useQuery<PagedBillings>({
    queryKey: billingsQueryKey(query),
    queryFn: () => listMockBillings(query),
    placeholderData: (previous) => previous,
  });
}
