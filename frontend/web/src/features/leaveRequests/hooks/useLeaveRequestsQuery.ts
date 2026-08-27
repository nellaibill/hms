import type { LeaveRequestListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { leaveRequestsApi } from '../../../services/apiClient';

export const leaveRequestsQueryKey = (query: LeaveRequestListQuery) => ['leaveRequests', 'list', query] as const;

export function useLeaveRequestsQuery(query: LeaveRequestListQuery) {
  return useQuery({
    queryKey: leaveRequestsQueryKey(query),
    queryFn: () => leaveRequestsApi.getLeaveRequests(query),
    placeholderData: (previous) => previous,
  });
}
