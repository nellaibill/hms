import type { ActivityLogListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { activityLogApi } from '../../../services/apiClient';

export function useActivityLogsQuery(query: ActivityLogListQuery) {
  return useQuery({
    queryKey: ['activity-log', 'list', query] as const,
    queryFn: () => activityLogApi.getActivityLogs(query),
    placeholderData: (previous) => previous,
  });
}

export function useActivityLogDetailQuery(id: string | null) {
  return useQuery({
    queryKey: ['activity-log', 'detail', id] as const,
    queryFn: () => activityLogApi.getActivityLogById(id as string),
    enabled: id !== null,
  });
}
