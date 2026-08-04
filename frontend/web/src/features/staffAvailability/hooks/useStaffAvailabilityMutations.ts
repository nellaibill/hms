import type { CreateStaffAvailabilityRequest, UpdateStaffAvailabilityRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';
import { createMockStaffAvailability, deleteMockStaffAvailability, updateMockStaffAvailability } from '../mockStaffAvailabilityStore';

function useInvalidateStaffAvailability() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['staffAvailability'] });
}

export function useCreateStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: async (request: CreateStaffAvailabilityRequest) => {
      try {
        return await staffAvailabilityApi.createStaffAvailability(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockStaffAvailability(request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useUpdateStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateStaffAvailabilityRequest }) => {
      try {
        return await staffAvailabilityApi.updateStaffAvailability(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockStaffAvailability(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useDeleteStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await staffAvailabilityApi.deleteStaffAvailability(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockStaffAvailability(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}
