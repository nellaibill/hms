import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';
import { getMockShiftById } from '../mockShiftsStore';

export function useShiftQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shifts', 'detail', id],
    queryFn: async () => {
      try {
        return await shiftsApi.getShiftById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockShiftById(id as string);
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
