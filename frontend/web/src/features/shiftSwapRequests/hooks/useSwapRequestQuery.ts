import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';
import { getMockSwapRequestById } from '../mockShiftSwapRequestsStore';

export function useSwapRequestQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['shiftSwapRequests', 'detail', id],
    queryFn: async () => {
      try {
        return await shiftSwapRequestsApi.getSwapRequestById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockSwapRequestById(id as string);
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
