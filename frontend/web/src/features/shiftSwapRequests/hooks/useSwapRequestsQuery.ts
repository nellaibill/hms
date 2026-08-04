import type { PagedSwapRequests, SwapRequestListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';
import { listMockSwapRequests } from '../mockShiftSwapRequestsStore';

export const swapRequestsQueryKey = (query: SwapRequestListQuery) => ['shiftSwapRequests', 'list', query] as const;

export type PagedSwapRequestsResult = PagedSwapRequests & { source: 'live' | 'mock' };

export function useSwapRequestsQuery(query: SwapRequestListQuery) {
  return useQuery({
    queryKey: swapRequestsQueryKey(query),
    queryFn: async (): Promise<PagedSwapRequestsResult> => {
      try {
        const result = await shiftSwapRequestsApi.getSwapRequests(query);
        return { ...result, source: 'live' };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...listMockSwapRequests(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
