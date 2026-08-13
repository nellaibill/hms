import type { CreateAdmissionChargeRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useAdmissionChargesQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'charges', admissionId],
    queryFn: () => admissionsApi.getCharges(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostAdmissionChargeMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateAdmissionChargeRequest) => admissionsApi.postCharge(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'charges', admissionId] }),
  });
}
