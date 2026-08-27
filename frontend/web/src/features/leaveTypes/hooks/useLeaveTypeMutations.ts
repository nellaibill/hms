import type { CreateLeaveTypeRequest, UpdateLeaveTypeRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { leaveTypesApi } from '../../../services/apiClient';

function useInvalidateLeaveTypes() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['leaveTypes'] });
}

export function useCreateLeaveTypeMutation() {
  const invalidate = useInvalidateLeaveTypes();
  return useMutation({
    mutationFn: (request: CreateLeaveTypeRequest) => leaveTypesApi.createLeaveType(request),
    onSuccess: invalidate,
  });
}

export function useUpdateLeaveTypeMutation() {
  const invalidate = useInvalidateLeaveTypes();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateLeaveTypeRequest }) => leaveTypesApi.updateLeaveType(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteLeaveTypeMutation() {
  const invalidate = useInvalidateLeaveTypes();
  return useMutation({
    mutationFn: (id: string) => leaveTypesApi.deleteLeaveType(id),
    onSuccess: invalidate,
  });
}
