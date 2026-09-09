import type { CreateAdmissionAdvanceRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useAdmissionAdvancesQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'advances', admissionId],
    queryFn: () => admissionsApi.getAdvances(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostAdmissionAdvanceMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateAdmissionAdvanceRequest) => admissionsApi.postAdvance(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'advances', admissionId] }),
  });
}
