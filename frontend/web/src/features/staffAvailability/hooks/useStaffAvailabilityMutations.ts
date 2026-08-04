import type { CreateStaffAvailabilityRequest, UpdateStaffAvailabilityRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { staffAvailabilityApi } from '../../../services/apiClient';

function useInvalidateStaffAvailability() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['staffAvailability'] });
}

export function useCreateStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: (request: CreateStaffAvailabilityRequest) => staffAvailabilityApi.createStaffAvailability(request),
    onSuccess: invalidate,
  });
}

export function useUpdateStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateStaffAvailabilityRequest }) =>
      staffAvailabilityApi.updateStaffAvailability(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteStaffAvailabilityMutation() {
  const invalidate = useInvalidateStaffAvailability();
  return useMutation({
    mutationFn: (id: string) => staffAvailabilityApi.deleteStaffAvailability(id),
    onSuccess: invalidate,
  });
}
