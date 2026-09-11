import type { OpdPatientListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { opdApi } from '@/services/apiClient';

export const opdPatientListQueryKey = (query: OpdPatientListQuery) => ['opd', 'patients', 'list', query] as const;

/** OPD Patient List tab — paged, searchable, filterable by date-range/department/consultant/status. */
export function useOpdPatientListQuery(query: OpdPatientListQuery) {
  return useQuery({
    queryKey: opdPatientListQueryKey(query),
    queryFn: () => opdApi.getPatientList(query),
    placeholderData: (previous) => previous,
  });
}
