import { useQuery } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useAdmissionQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'detail', id],
    queryFn: () => admissionsApi.getAdmissionById(id as string),
    enabled: Boolean(id),
  });
}
