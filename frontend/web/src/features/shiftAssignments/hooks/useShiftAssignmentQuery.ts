import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';
import { getMockShiftAssignmentById } from '../mockShiftAssignmentsStore';

export function useShiftAssignmentQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shiftAssignments', 'detail', id],
    queryFn: async () => {
      try {
        return await shiftAssignmentsApi.getShiftAssignmentById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockShiftAssignmentById(id as string);
          if (mock) {
            return mock;
          }
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
