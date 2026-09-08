import type { FinalizeDischargeSummaryRequest, UpdateDischargeSummaryRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { dischargeSummaryApi } from '@/services/apiClient';

function useInvalidateDischargeSummary() {
  const queryClient = useQueryClient();
  return (admissionId?: string) => {
    if (admissionId) {
      queryClient.invalidateQueries({ queryKey: ['discharge-summary', 'by-admission', admissionId] });
    }
  };
}

/** Creates the Draft for an already-discharged admission — the AdmissionViewPage entry point's "Create Discharge Summary" action. */
export function useCreateDischargeSummaryMutation() {
  const invalidate = useInvalidateDischargeSummary();
  return useMutation({
    mutationFn: (admissionId: string) => dischargeSummaryApi.createDraft(admissionId),
    onSuccess: (data) => invalidate(data.admissionId),
  });
}

export function useUpdateDischargeSummaryMutation() {
  const invalidate = useInvalidateDischargeSummary();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDischargeSummaryRequest }) => dischargeSummaryApi.update(id, request),
    onSuccess: (data) => invalidate(data.admissionId),
  });
}

/** Locks the record — irreversible, gated behind a confirmation dialog in the UI. */
export function useFinalizeDischargeSummaryMutation() {
  const invalidate = useInvalidateDischargeSummary();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: FinalizeDischargeSummaryRequest }) => dischargeSummaryApi.finalize(id, request),
    onSuccess: (data) => invalidate(data.admissionId),
  });
}
