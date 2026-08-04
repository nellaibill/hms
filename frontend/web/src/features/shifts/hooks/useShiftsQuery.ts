import type { ShiftListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { shiftsApi } from '../../../services/apiClient';

export const shiftsQueryKey = (query: ShiftListQuery) => ['shifts', 'list', query] as const;

export function useShiftsQuery(query: ShiftListQuery) {
  return useQuery({
    queryKey: shiftsQueryKey(query),
    queryFn: () => shiftsApi.getShifts(query),
    placeholderData: (previous) => previous,
  });
}
