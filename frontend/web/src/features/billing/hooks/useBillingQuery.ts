import { useQuery } from '@tanstack/react-query';
import { getInvoiceById } from '../apiBillingRepository';

export function useBillingQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['billings', 'detail', id],
    queryFn: () => getInvoiceById(id as string),
    enabled: Boolean(id),
    // A discount waiting for Super Admin approval (ADR-109) is decided on another screen —
    // check back every 15 s so the counter sees the decision without reloading.
    refetchInterval: (query) => (query.state.data?.discountStatus === 'Pending' ? 15_000 : false),
  });
}
