import type { CreateDiagnosticCategoryRequest, UpdateDiagnosticCategoryRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { diagnosticCategoriesApi } from '../../../services/apiClient';

function useInvalidateDiagnosticCategories() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['diagnostics', 'categories'] });
}

export function useCreateDiagnosticCategoryMutation() {
  const invalidate = useInvalidateDiagnosticCategories();
  return useMutation({
    mutationFn: (request: CreateDiagnosticCategoryRequest) => diagnosticCategoriesApi.createDiagnosticCategory(request),
    onSuccess: invalidate,
  });
}

export function useUpdateDiagnosticCategoryMutation() {
  const invalidate = useInvalidateDiagnosticCategories();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDiagnosticCategoryRequest }) =>
      diagnosticCategoriesApi.updateDiagnosticCategory(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteDiagnosticCategoryMutation() {
  const invalidate = useInvalidateDiagnosticCategories();
  return useMutation({
    mutationFn: (id: string) => diagnosticCategoriesApi.deleteDiagnosticCategory(id),
    onSuccess: invalidate,
  });
}
