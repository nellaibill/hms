import type { CreateNursingAssessmentRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useNursingAssessmentsQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'nursing-assessments', admissionId],
    queryFn: () => admissionsApi.getNursingAssessments(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostNursingAssessmentMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateNursingAssessmentRequest) => admissionsApi.postNursingAssessment(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'nursing-assessments', admissionId] }),
  });
}
