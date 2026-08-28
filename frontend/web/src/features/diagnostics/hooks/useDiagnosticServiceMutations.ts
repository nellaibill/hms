import type { CreateDiagnosticServiceRequest, UpdateDiagnosticServiceRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { diagnosticServicesApi } from '../../../services/apiClient';

function useInvalidateDiagnosticServices() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['diagnostics', 'services'] });
}

export function useCreateDiagnosticServiceMutation() {
  const invalidate = useInvalidateDiagnosticServices();
  return useMutation({
    mutationFn: (request: CreateDiagnosticServiceRequest) => diagnosticServicesApi.createDiagnosticService(request),
    onSuccess: invalidate,
  });
}

export function useUpdateDiagnosticServiceMutation() {
  const invalidate = useInvalidateDiagnosticServices();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDiagnosticServiceRequest }) =>
      diagnosticServicesApi.updateDiagnosticService(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteDiagnosticServiceMutation() {
  const invalidate = useInvalidateDiagnosticServices();
  return useMutation({
    mutationFn: (id: string) => diagnosticServicesApi.deleteDiagnosticService(id),
    onSuccess: invalidate,
  });
}
