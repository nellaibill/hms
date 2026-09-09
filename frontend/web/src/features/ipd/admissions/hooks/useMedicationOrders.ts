import type { CreateMedicationOrderRequest, DiscontinueMedicationOrderRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

function medicationOrdersKey(admissionId: string | undefined) {
  return ['ipd', 'admissions', 'medication-orders', admissionId];
}

export function useMedicationOrdersQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: medicationOrdersKey(admissionId),
    queryFn: () => admissionsApi.getMedicationOrders(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostMedicationOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateMedicationOrderRequest) => admissionsApi.postMedicationOrder(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: medicationOrdersKey(admissionId) }),
  });
}

export function useDiscontinueMedicationOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderId, request }: { orderId: string; request?: DiscontinueMedicationOrderRequest }) =>
      admissionsApi.discontinueMedicationOrder(admissionId, orderId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: medicationOrdersKey(admissionId) }),
  });
}
