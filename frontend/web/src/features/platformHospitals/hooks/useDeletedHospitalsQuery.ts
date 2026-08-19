import type { TenantListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useDeletedHospitalsQuery(query: TenantListQuery, enabled = true) {
  return useQuery({
    queryKey: ['platformDeletedHospitals', query],
    queryFn: () => platformHospitalsApi.getDeletedHospitals(query),
    enabled,
  });
}
