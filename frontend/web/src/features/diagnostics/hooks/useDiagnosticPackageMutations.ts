import type { AddDiagnosticPackageItemRequest, CreateDiagnosticPackageRequest, UpdateDiagnosticPackageRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { diagnosticPackagesApi } from '../../../services/apiClient';

function useInvalidateDiagnosticPackages() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['diagnostics', 'packages'] });
}

export function useCreateDiagnosticPackageMutation() {
  const invalidate = useInvalidateDiagnosticPackages();
  return useMutation({
    mutationFn: (request: CreateDiagnosticPackageRequest) => diagnosticPackagesApi.createDiagnosticPackage(request),
    onSuccess: invalidate,
  });
}

export function useUpdateDiagnosticPackageMutation() {
  const invalidate = useInvalidateDiagnosticPackages();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDiagnosticPackageRequest }) =>
      diagnosticPackagesApi.updateDiagnosticPackage(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteDiagnosticPackageMutation() {
  const invalidate = useInvalidateDiagnosticPackages();
  return useMutation({
    mutationFn: (id: string) => diagnosticPackagesApi.deleteDiagnosticPackage(id),
    onSuccess: invalidate,
  });
}

export function useAddPackageItemMutation() {
  const invalidate = useInvalidateDiagnosticPackages();
  return useMutation({
    mutationFn: ({ packageId, request }: { packageId: string; request: AddDiagnosticPackageItemRequest }) =>
      diagnosticPackagesApi.addDiagnosticPackageItem(packageId, request),
    onSuccess: invalidate,
  });
}

export function useRemovePackageItemMutation() {
  const invalidate = useInvalidateDiagnosticPackages();
  return useMutation({
    mutationFn: ({ packageId, itemId }: { packageId: string; itemId: string }) =>
      diagnosticPackagesApi.removeDiagnosticPackageItem(packageId, itemId),
    onSuccess: invalidate,
  });
}
