import { useQuery } from '@tanstack/react-query';
import { getLabOrderById } from '../apiLaboratoryRepository';

export const labOrderQueryKey = (id: string | undefined) => ['labOrders', 'detail', id] as const;

export function useLabOrderQuery(id: string | undefined) {
  return useQuery({
    queryKey: labOrderQueryKey(id),
    queryFn: () => getLabOrderById(id as string),
    enabled: Boolean(id),
  });
}
