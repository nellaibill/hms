import type { PagedPatients, PatientListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

export const patientsQueryKey = (query: PatientListQuery) => ['patients', 'list', query] as const;

export function usePatientsQuery(query: PatientListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: patientsQueryKey(query),
    queryFn: (): Promise<PagedPatients> => patientsApi.getPatients(query),
    enabled: options?.enabled ?? true,
    placeholderData: (previous) => previous,
  });
}
