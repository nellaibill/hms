import type { ShiftAssignmentListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';

export const shiftAssignmentsQueryKey = (query: ShiftAssignmentListQuery) => ['shiftAssignments', 'list', query] as const;

export function useShiftAssignmentsQuery(query: ShiftAssignmentListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: shiftAssignmentsQueryKey(query),
    queryFn: () => shiftAssignmentsApi.getShiftAssignments(query),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });
}
