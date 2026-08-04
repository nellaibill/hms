import { useQuery } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';

export function useShiftAssignmentQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shiftAssignments', 'detail', id],
    queryFn: () => shiftAssignmentsApi.getShiftAssignmentById(id as string),
    enabled: Boolean(id),
  });
}
