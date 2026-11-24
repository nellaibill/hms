import type { ShiftListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';

export const shiftsQueryKey = (query: ShiftListQuery) => ['shifts', 'list', query] as const;

export function useShiftsQuery(query: ShiftListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: shiftsQueryKey(query),
    queryFn: () => shiftsApi.getShifts(query),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });
}
