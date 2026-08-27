import type { ApproveLeaveRequestRequest, CreateLeaveRequestRequest, RejectLeaveRequestRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { leaveRequestsApi } from '../../../services/apiClient';

function useInvalidateLeaveRequests() {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ['leaveRequests'] });
    // Approving/rejecting/cancelling can change an employee's used/remaining leave balance.
    queryClient.invalidateQueries({ queryKey: ['employees', 'leave-balances'] });
  };
}

export function useCreateLeaveRequestMutation() {
  const invalidate = useInvalidateLeaveRequests();
  return useMutation({
    mutationFn: (request: CreateLeaveRequestRequest) => leaveRequestsApi.createLeaveRequest(request),
    onSuccess: invalidate,
  });
}

export function useApproveLeaveRequestMutation() {
  const invalidate = useInvalidateLeaveRequests();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: ApproveLeaveRequestRequest }) => leaveRequestsApi.approveLeaveRequest(id, request),
    onSuccess: invalidate,
  });
}

export function useRejectLeaveRequestMutation() {
  const invalidate = useInvalidateLeaveRequests();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RejectLeaveRequestRequest }) => leaveRequestsApi.rejectLeaveRequest(id, request),
    onSuccess: invalidate,
  });
}

export function useCancelLeaveRequestMutation() {
  const invalidate = useInvalidateLeaveRequests();
  return useMutation({
    mutationFn: (id: string) => leaveRequestsApi.cancelLeaveRequest(id),
    onSuccess: invalidate,
  });
}
