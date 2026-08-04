import type { CreateSwapRequest, UpdateSwapRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftSwapRequestsApi } from '../../../services/apiClient';
import { createMockSwapRequest, deleteMockSwapRequest, updateMockSwapRequest } from '../mockShiftSwapRequestsStore';

function useInvalidateSwapRequests() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shiftSwapRequests'] });
}

export function useCreateSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: async (request: CreateSwapRequest) => {
      try {
        return await shiftSwapRequestsApi.createSwapRequest(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockSwapRequest(request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useUpdateSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateSwapRequest }) => {
      try {
        return await shiftSwapRequestsApi.updateSwapRequest(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockSwapRequest(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useDeleteSwapRequestMutation() {
  const invalidate = useInvalidateSwapRequests();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await shiftSwapRequestsApi.deleteSwapRequest(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockSwapRequest(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}
