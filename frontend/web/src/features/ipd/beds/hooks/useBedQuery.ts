import { useQuery } from '@tanstack/react-query';
import { bedsApi } from '../../../../services/apiClient';

export function useBedQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'beds', 'detail', id],
    queryFn: () => bedsApi.getBedById(id as string),
    enabled: Boolean(id),
  });
}
