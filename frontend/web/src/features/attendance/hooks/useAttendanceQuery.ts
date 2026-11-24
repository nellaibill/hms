import type { AttendanceListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { attendanceApi } from '../../../services/apiClient';

export const attendanceQueryKey = (query: AttendanceListQuery) => ['attendance', 'list', query] as const;

export function useAttendanceQuery(query: AttendanceListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: attendanceQueryKey(query),
    queryFn: () => attendanceApi.getAttendance(query),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });
}
