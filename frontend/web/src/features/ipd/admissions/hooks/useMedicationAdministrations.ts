import type { CreateMedicationAdministrationRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

function medicationAdministrationsKey(admissionId: string | undefined, orderId: string | undefined) {
  return ['ipd', 'admissions', 'medication-administrations', admissionId, orderId];
}

export function useMedicationAdministrationsQuery(admissionId: string | undefined, orderId: string | undefined) {
  return useQuery({
    queryKey: medicationAdministrationsKey(admissionId, orderId),
    queryFn: () => admissionsApi.getMedicationAdministrations(admissionId as string, orderId as string),
    enabled: Boolean(admissionId) && Boolean(orderId),
  });
}

export function usePostMedicationAdministrationMutation(admissionId: string, orderId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateMedicationAdministrationRequest) => admissionsApi.postMedicationAdministration(admissionId, orderId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: medicationAdministrationsKey(admissionId, orderId) }),
  });
}
