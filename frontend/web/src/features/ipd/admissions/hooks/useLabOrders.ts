import type { CreatePlaceLabOrderRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

function labOrdersKey(admissionId: string | undefined) {
  return ['ipd', 'admissions', 'lab-orders', admissionId];
}

export function useLabOrdersQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: labOrdersKey(admissionId),
    queryFn: () => admissionsApi.getLabOrders(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostLabOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePlaceLabOrderRequest) => admissionsApi.postLabOrder(admissionId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: labOrdersKey(admissionId) });
      // A successful order also auto-posts one AdmissionCharge per line (see
      // IPDLabOrderService.PlaceOrderAsync) — refresh the Charges tab too.
      queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'charges', admissionId] });
    },
  });
}
