import type { WeeklyRosterListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';

export const weeklyRostersQueryKey = (query: WeeklyRosterListQuery) => ['weeklyRosters', 'list', query] as const;

export function useWeeklyRostersQuery(query: WeeklyRosterListQuery) {
  return useQuery({
    queryKey: weeklyRostersQueryKey(query),
    queryFn: () => weeklyRostersApi.getWeeklyRosters(query),
    placeholderData: (previous) => previous,
  });
}
