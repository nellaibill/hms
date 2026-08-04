import type { SwapRequestListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';

export const swapRequestsQueryKey = (query: SwapRequestListQuery) => ['shiftSwapRequests', 'list', query] as const;

export function useSwapRequestsQuery(query: SwapRequestListQuery) {
  return useQuery({
    queryKey: swapRequestsQueryKey(query),
    queryFn: () => shiftSwapRequestsApi.getSwapRequests(query),
    placeholderData: (previous) => previous,
  });
}
