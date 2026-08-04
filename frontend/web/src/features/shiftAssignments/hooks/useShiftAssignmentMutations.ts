import type { CreateShiftAssignmentRequest, UpdateShiftAssignmentRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';
import { createMockShiftAssignment, deleteMockShiftAssignment, updateMockShiftAssignment } from '../mockShiftAssignmentsStore';

function useInvalidateShiftAssignments() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shiftAssignments'] });
}

export function useCreateShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: async (request: CreateShiftAssignmentRequest) => {
      try {
        return await shiftAssignmentsApi.createShiftAssignment(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockShiftAssignment(request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useUpdateShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateShiftAssignmentRequest }) => {
      try {
        return await shiftAssignmentsApi.updateShiftAssignment(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockShiftAssignment(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useDeleteShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await shiftAssignmentsApi.deleteShiftAssignment(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockShiftAssignment(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}
