import type { MonthlyWeeklyRosterQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';

export const monthlyWeeklyRostersQueryKey = (query: MonthlyWeeklyRosterQuery) => ['weeklyRosters', 'monthly', query] as const;

export function useMonthlyWeeklyRostersQuery(query: MonthlyWeeklyRosterQuery) {
  return useQuery({
    queryKey: monthlyWeeklyRostersQueryKey(query),
    queryFn: () => weeklyRostersApi.getMonthlyWeeklyRosters(query),
    placeholderData: (previous) => previous,
  });
}
