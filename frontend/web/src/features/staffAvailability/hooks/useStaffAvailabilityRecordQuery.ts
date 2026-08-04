import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';
import { getMockStaffAvailabilityById } from '../mockStaffAvailabilityStore';

export function useStaffAvailabilityRecordQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['staffAvailability', 'detail', id],
    queryFn: async () => {
      try {
        return await staffAvailabilityApi.getStaffAvailabilityById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockStaffAvailabilityById(id as string);
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
