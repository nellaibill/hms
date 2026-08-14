import type { TenantListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useHospitalsQuery(query: TenantListQuery) {
  return useQuery({
    queryKey: ['platformHospitals', query],
    queryFn: () => platformHospitalsApi.getHospitals(query),
  });
}

export function useHospitalStatsQuery() {
  return useQuery({
    queryKey: ['platformHospitalStats'],
    queryFn: () => platformHospitalsApi.getStats(),
  });
}
