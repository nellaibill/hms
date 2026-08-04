import type { CreateSwapRequest, UpdateSwapRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';

function useInvalidateSwapRequests() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shiftSwapRequests'] });
}

export function useCreateSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: (request: CreateSwapRequest) => shiftSwapRequestsApi.createSwapRequest(request),
    onSuccess: invalidate,
  });
}

export function useUpdateSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateSwapRequest }) => shiftSwapRequestsApi.updateSwapRequest(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: (id: string) => shiftSwapRequestsApi.deleteSwapRequest(id),
    onSuccess: invalidate,
  });
}
