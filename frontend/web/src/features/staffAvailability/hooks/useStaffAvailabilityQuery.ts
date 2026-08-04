import type { StaffAvailabilityListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';

export const staffAvailabilityQueryKey = (query: StaffAvailabilityListQuery) => ['staffAvailability', 'list', query] as const;

export function useStaffAvailabilityQuery(query: StaffAvailabilityListQuery) {
  return useQuery({
    queryKey: staffAvailabilityQueryKey(query),
    queryFn: () => staffAvailabilityApi.getStaffAvailability(query),
    placeholderData: (previous) => previous,
  });
}
