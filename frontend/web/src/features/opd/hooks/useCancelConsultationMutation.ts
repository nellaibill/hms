import { useMutation, useQueryClient } from '@tanstack/react-query';
import { opdApi } from '@/services/apiClient';

/** Backs the OPD Patient List's "Patient left" action: cancels a consultation that hasn't started
 * (its visit's bill was cancelled and the patient has gone), so it leaves the doctor's queue. */
export function useCancelConsultationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (consultationId: string) => opdApi.cancel(consultationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opd', 'patients'] });
      queryClient.invalidateQueries({ queryKey: ['opd', 'consultations'] });
    },
  });
}
