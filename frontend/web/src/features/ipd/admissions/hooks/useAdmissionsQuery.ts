import type { AdmissionListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export const admissionsQueryKey = (query: AdmissionListQuery) => ['ipd', 'admissions', 'list', query] as const;

export function useAdmissionsQuery(query: AdmissionListQuery) {
  return useQuery({
    queryKey: admissionsQueryKey(query),
    queryFn: () => admissionsApi.getAdmissions(query),
    placeholderData: (previous) => previous,
  });
}
