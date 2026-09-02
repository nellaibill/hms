import { useMutation, useQueryClient } from '@tanstack/react-query';
import { patientImportApi } from '../../../services/apiClient';
import { patientImportBatchQueryKey } from './usePatientImportBatchQuery';

export function useUploadPatientImportMutation() {
  return useMutation({
    mutationFn: (file: File) => patientImportApi.upload(file),
  });
}

export function useCommitPatientImportBatchMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (batchId: string) => patientImportApi.commit(batchId),
    onSuccess: (_data, batchId) => queryClient.invalidateQueries({ queryKey: patientImportBatchQueryKey(batchId) }),
  });
}
