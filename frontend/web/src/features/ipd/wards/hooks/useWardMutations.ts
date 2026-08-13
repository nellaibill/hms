import type { CreateWardRequest, UpdateWardRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { wardsApi } from '../../../../services/apiClient';

function useInvalidateWards() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['ipd', 'wards'] });
}

export function useCreateWardMutation() {
  const invalidateWards = useInvalidateWards();
  return useMutation({
    mutationFn: (request: CreateWardRequest) => wardsApi.createWard(request),
    onSuccess: invalidateWards,
  });
}

export function useUpdateWardMutation() {
  const invalidateWards = useInvalidateWards();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateWardRequest }) => wardsApi.updateWard(id, request),
    onSuccess: invalidateWards,
  });
}

export function useDeleteWardMutation() {
  const invalidateWards = useInvalidateWards();
  return useMutation({
    mutationFn: (id: string) => wardsApi.deleteWard(id),
    onSuccess: invalidateWards,
  });
}
