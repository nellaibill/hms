import type { CreateDiagnosticProviderRequest, UpdateDiagnosticProviderRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { diagnosticProvidersApi } from '../../../services/apiClient';

function useInvalidateDiagnosticProviders() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['diagnostics', 'providers'] });
}

export function useCreateDiagnosticProviderMutation() {
  const invalidate = useInvalidateDiagnosticProviders();
  return useMutation({
    mutationFn: (request: CreateDiagnosticProviderRequest) => diagnosticProvidersApi.createDiagnosticProvider(request),
    onSuccess: invalidate,
  });
}

export function useUpdateDiagnosticProviderMutation() {
  const invalidate = useInvalidateDiagnosticProviders();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDiagnosticProviderRequest }) =>
      diagnosticProvidersApi.updateDiagnosticProvider(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteDiagnosticProviderMutation() {
  const invalidate = useInvalidateDiagnosticProviders();
  return useMutation({
    mutationFn: (id: string) => diagnosticProvidersApi.deleteDiagnosticProvider(id),
    onSuccess: invalidate,
  });
}
