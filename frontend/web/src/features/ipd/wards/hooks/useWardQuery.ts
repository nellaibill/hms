import { useQuery } from '@tanstack/react-query';
import { wardsApi } from '../../../../services/apiClient';

export function useWardQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'wards', 'detail', id],
    queryFn: () => wardsApi.getWardById(id as string),
    enabled: Boolean(id),
  });
}
