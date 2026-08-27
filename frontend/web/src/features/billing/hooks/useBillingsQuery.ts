import { useQuery } from '@tanstack/react-query';
import { listInvoices, type BillingListQuery, type PagedBillings } from '../apiBillingRepository';

export const billingsQueryKey = (query: BillingListQuery) => ['billings', 'list', query] as const;

export function useBillingsQuery(query: BillingListQuery) {
  return useQuery<PagedBillings>({
    queryKey: billingsQueryKey(query),
    queryFn: () => listInvoices(query),
    placeholderData: (previous) => previous,
  });
}
