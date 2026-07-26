import type { PatientListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

export const patientsQueryKey = (query: PatientListQuery) => ['patients', 'list', query] as const;

export function usePatientsQuery(query: PatientListQuery) {
  return useQuery({
    queryKey: patientsQueryKey(query),
    queryFn: () => patientsApi.getPatients(query),
    placeholderData: (previous) => previous,
  });
}
