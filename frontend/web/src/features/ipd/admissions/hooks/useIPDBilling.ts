import { useMutation, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useGenerateFinalBillMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => admissionsApi.postFinalBill(admissionId),
    // Refetch the admission so admission.finalInvoiceId flips the Billing tab from
    // "Generate" to "View" without a manual reload — mirrors useAdmissionQuery's own key.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'detail', admissionId] }),
  });
}
