import { useMutation, useQueryClient } from '@tanstack/react-query';
import { opdApi } from '@/services/apiClient';

/** Backs the OPD Patient List tab's "Consult" action (Waiting/CheckedIn rows) — moves the
 * consultation to InConsultation, then the caller navigates to the patient record. */
export function useStartConsultationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (consultationId: string) => opdApi.startConsultation(consultationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opd', 'patients'] });
      queryClient.invalidateQueries({ queryKey: ['opd', 'consultations'] });
    },
  });
}
