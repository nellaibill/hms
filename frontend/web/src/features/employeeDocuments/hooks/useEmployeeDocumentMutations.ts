import type { DocumentType } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { documentsApi } from '../../../services/apiClient';

interface UploadEmployeeDocumentInput {
  file: File;
  documentType: DocumentType;
  expiryDate?: string | null;
}

export function useUploadEmployeeDocumentMutation(employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, documentType, expiryDate }: UploadEmployeeDocumentInput) =>
      documentsApi.uploadDocument(file, { ownerType: 'Staff', ownerId: employeeId, documentType, expiryDate }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employeeDocuments', 'list', employeeId] }),
  });
}

export function useDeleteEmployeeDocumentMutation(employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => documentsApi.deleteDocument(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employeeDocuments', 'list', employeeId] }),
  });
}
