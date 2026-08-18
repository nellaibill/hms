import { useQuery } from '@tanstack/react-query';
import { getInvoiceById } from '../apiBillingRepository';

export function useBillingQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['billings', 'detail', id],
    queryFn: () => getInvoiceById(id as string),
    enabled: Boolean(id),
  });
}
