import type { SaveOpdConsultationRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

function useInvalidateOpdConsultation(consultationId: string | undefined) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ['opd-consultation', consultationId] });
  };
}

export function useSaveOpdConsultationDraftMutation(consultationId: string | undefined) {
  const invalidate = useInvalidateOpdConsultation(consultationId);
  return useMutation({
    mutationFn: (request: SaveOpdConsultationRequest) => opdConsultationApi.saveDraft(consultationId as string, request),
    onSuccess: invalidate,
  });
}

/** Also invalidates the OPD Patient List's own queries — Complete advances the owning
 * PatientVisitConsultation's queue status too (see the backend's CompleteAsync), so the list
 * should stop offering "Consult" for this row the next time it's viewed, same invalidation
 * useStartConsultationMutation already does for the same reason. */
export function useCompleteOpdConsultationMutation(consultationId: string | undefined) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateOpdConsultation(consultationId);
  return useMutation({
    mutationFn: (request: SaveOpdConsultationRequest) => opdConsultationApi.complete(consultationId as string, request),
    onSuccess: () => {
      invalidate();
      queryClient.invalidateQueries({ queryKey: ['opd', 'patients'] });
      queryClient.invalidateQueries({ queryKey: ['opd', 'consultations'] });
    },
  });
}

/** The inverse of useCompleteOpdConsultationMutation — moves a Completed note back to Draft so
 * it can be edited again, reverting the owning PatientVisitConsultation's queue status back to
 * InConsultation on the backend, so the same list invalidations apply here too. */
export function useReopenOpdConsultationMutation(consultationId: string | undefined) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateOpdConsultation(consultationId);
  return useMutation({
    mutationFn: () => opdConsultationApi.reopen(consultationId as string),
    onSuccess: () => {
      invalidate();
      queryClient.invalidateQueries({ queryKey: ['opd', 'patients'] });
      queryClient.invalidateQueries({ queryKey: ['opd', 'consultations'] });
    },
  });
}
