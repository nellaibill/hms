import { useQuery } from '@tanstack/react-query';
import { getMockBillingById } from '../mockBillingStore';

export function useBillingQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['billings', 'detail', id],
    queryFn: () => {
      const billing = getMockBillingById(id as string);
      if (!billing) {
        throw new Error(`Invoice ${id} not found.`);
      }
      return billing;
    },
    enabled: Boolean(id),
  });
}
