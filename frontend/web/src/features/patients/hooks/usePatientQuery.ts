import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

export function usePatientQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['patients', 'detail', id],
    queryFn: () => patientsApi.getPatientById(id as string),
    enabled: Boolean(id),
  });
}
