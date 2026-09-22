import type { XrayAiAnalysisResponse } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { radiologyApi } from '../../../services/apiClient';
import { patientXrayAnalysesQueryKey } from './usePatientXrayAnalysesQuery';

/** Marks a saved AI draft as reviewed and patches it in the cached list so the badge flips at once. */
export function useMarkAnalysisReviewedMutation(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (analysisId: string) => radiologyApi.markReviewed(analysisId),
    onSuccess: (reviewed) => {
      queryClient.setQueryData<XrayAiAnalysisResponse[]>(patientXrayAnalysesQueryKey(patientId), (current) =>
        current?.map((analysis) => (analysis.id === reviewed.id ? reviewed : analysis)),
      );
    },
  });
}
