import { useQuery } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useBedStayHistoryQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'bed-history', admissionId],
    queryFn: () => admissionsApi.getBedStayHistory(admissionId as string),
    enabled: Boolean(admissionId),
  });
}
