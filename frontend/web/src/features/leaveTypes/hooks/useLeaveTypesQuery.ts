import type { LeaveTypeListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { leaveTypesApi } from '../../../services/apiClient';

export const leaveTypesQueryKey = (query: LeaveTypeListQuery) => ['leaveTypes', 'list', query] as const;

export function useLeaveTypesQuery(query: LeaveTypeListQuery) {
  return useQuery({
    queryKey: leaveTypesQueryKey(query),
    queryFn: () => leaveTypesApi.getLeaveTypes(query),
    placeholderData: (previous) => previous,
  });
}
