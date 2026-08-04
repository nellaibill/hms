import { useQuery } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';

export function useShiftQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shifts', 'detail', id],
    queryFn: () => shiftsApi.getShiftById(id as string),
    enabled: Boolean(id),
  });
}
