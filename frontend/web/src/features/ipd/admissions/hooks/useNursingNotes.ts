import type { CreateNursingNoteRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useNursingNotesQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'nursing-notes', admissionId],
    queryFn: () => admissionsApi.getNursingNotes(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostNursingNoteMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateNursingNoteRequest) => admissionsApi.postNursingNote(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'nursing-notes', admissionId] }),
  });
}
