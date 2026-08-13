import { useQuery } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useTransferHistoryQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'transfer-history', admissionId],
    queryFn: () => admissionsApi.getTransferHistory(admissionId as string),
    enabled: Boolean(admissionId),
  });
}
