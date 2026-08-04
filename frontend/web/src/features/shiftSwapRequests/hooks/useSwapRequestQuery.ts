import { useQuery } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';

export function useSwapRequestQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shiftSwapRequests', 'detail', id],
    queryFn: () => shiftSwapRequestsApi.getSwapRequestById(id as string),
    enabled: Boolean(id),
  });
}
