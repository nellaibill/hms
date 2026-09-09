import type { CreateProgressNoteRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

export function useProgressNotesQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'progress-notes', admissionId],
    queryFn: () => admissionsApi.getProgressNotes(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostProgressNoteMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProgressNoteRequest) => admissionsApi.postProgressNote(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'progress-notes', admissionId] }),
  });
}
