import type { CreateShiftRequest, UpdateShiftRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';

function useInvalidateShifts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shifts'] });
}

export function useCreateShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: (request: CreateShiftRequest) => shiftsApi.createShift(request),
    onSuccess: invalidateShifts,
  });
}

export function useUpdateShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateShiftRequest }) => shiftsApi.updateShift(id, request),
    onSuccess: invalidateShifts,
  });
}

export function useDeleteShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: (id: string) => shiftsApi.deleteShift(id),
    onSuccess: invalidateShifts,
  });
}
