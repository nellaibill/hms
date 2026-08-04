import type { PagedShifts, ShiftListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';
import { listMockShifts } from '../mockShiftsStore';

export const shiftsQueryKey = (query: ShiftListQuery) => ['shifts', 'list', query] as const;

export type PagedShiftsResult = PagedShifts & { source: 'live' | 'mock' };

export function useShiftsQuery(query: ShiftListQuery) {
  return useQuery({
    queryKey: shiftsQueryKey(query),
    queryFn: async (): Promise<PagedShiftsResult> => {
      try {
        const result = await shiftsApi.getShifts(query);
        return { ...result, source: 'live' };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...listMockShifts(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
