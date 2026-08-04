import { useQuery } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';

export function useStaffAvailabilityRecordQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['staffAvailability', 'detail', id],
    queryFn: () => staffAvailabilityApi.getStaffAvailabilityById(id as string),
    enabled: Boolean(id),
  });
}
