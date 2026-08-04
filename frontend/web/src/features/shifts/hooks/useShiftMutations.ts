import type { CreateShiftRequest, UpdateShiftRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';
import { createMockShift, deleteMockShift, updateMockShift } from '../mockShiftsStore';

function useInvalidateShifts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shifts'] });
}

export function useCreateShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: async (request: CreateShiftRequest) => {
      try {
        return await shiftsApi.createShift(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockShift(request);
        }
        throw err;
      }
    },
    onSuccess: invalidateShifts,
  });
}

export function useUpdateShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateShiftRequest }) => {
      try {
        return await shiftsApi.updateShift(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockShift(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidateShifts,
  });
}

export function useDeleteShiftMutation() {
  const invalidateShifts = useInvalidateShifts();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await shiftsApi.deleteShift(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockShift(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidateShifts,
  });
}
