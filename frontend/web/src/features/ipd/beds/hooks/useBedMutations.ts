import type { CreateBedRequest, UpdateBedRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { bedsApi } from '../../../../services/apiClient';

function useInvalidateBeds() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['ipd', 'beds'] });
}

export function useCreateBedMutation() {
  const invalidateBeds = useInvalidateBeds();
  return useMutation({
    mutationFn: (request: CreateBedRequest) => bedsApi.createBed(request),
    onSuccess: invalidateBeds,
  });
}

export function useUpdateBedMutation() {
  const invalidateBeds = useInvalidateBeds();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateBedRequest }) => bedsApi.updateBed(id, request),
    onSuccess: invalidateBeds,
  });
}

export function useDeleteBedMutation() {
  const invalidateBeds = useInvalidateBeds();
  return useMutation({
    mutationFn: (id: string) => bedsApi.deleteBed(id),
    onSuccess: invalidateBeds,
  });
}
