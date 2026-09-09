import type { CreateVitalsReadingRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useVitalsReadingsQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'vitals', admissionId],
    queryFn: () => admissionsApi.getVitals(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostVitalsReadingMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateVitalsReadingRequest) => admissionsApi.postVitals(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'vitals', admissionId] }),
  });
}
