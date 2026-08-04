import type { PagedShiftAssignments, ShiftAssignmentListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';
import { listMockShiftAssignments } from '../mockShiftAssignmentsStore';

export const shiftAssignmentsQueryKey = (query: ShiftAssignmentListQuery) => ['shiftAssignments', 'list', query] as const;

export type PagedShiftAssignmentsResult = PagedShiftAssignments & { source: 'live' | 'mock' };

export function useShiftAssignmentsQuery(query: ShiftAssignmentListQuery) {
  return useQuery({
    queryKey: shiftAssignmentsQueryKey(query),
    queryFn: async (): Promise<PagedShiftAssignmentsResult> => {
      try {
        const result = await shiftAssignmentsApi.getShiftAssignments(query);
        return { ...result, source: 'live' };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...listMockShiftAssignments(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
