import type { PagedStaffAvailability, StaffAvailabilityListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';
import { listMockStaffAvailability } from '../mockStaffAvailabilityStore';

export const staffAvailabilityQueryKey = (query: StaffAvailabilityListQuery) => ['staffAvailability', 'list', query] as const;

export type PagedStaffAvailabilityResult = PagedStaffAvailability & { source: 'live' | 'mock' };

export function useStaffAvailabilityQuery(query: StaffAvailabilityListQuery) {
  return useQuery({
    queryKey: staffAvailabilityQueryKey(query),
    queryFn: async (): Promise<PagedStaffAvailabilityResult> => {
      try {
        const result = await staffAvailabilityApi.getStaffAvailability(query);
        return { ...result, source: 'live' };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...listMockStaffAvailability(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
