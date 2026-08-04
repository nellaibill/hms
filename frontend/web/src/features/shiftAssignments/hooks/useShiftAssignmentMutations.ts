import type { CreateShiftAssignmentRequest, UpdateShiftAssignmentRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shiftAssignmentsApi } from '../../../services/apiClient';

function useInvalidateShiftAssignments() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['shiftAssignments'] });
}

export function useCreateShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: (request: CreateShiftAssignmentRequest) => shiftAssignmentsApi.createShiftAssignment(request),
    onSuccess: invalidate,
  });
}

export function useUpdateShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateShiftAssignmentRequest }) =>
      shiftAssignmentsApi.updateShiftAssignment(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteShiftAssignmentMutation() {
  const invalidate = useInvalidateShiftAssignments();
  return useMutation({
    mutationFn: (id: string) => shiftAssignmentsApi.deleteShiftAssignment(id),
    onSuccess: invalidate,
  });
}
