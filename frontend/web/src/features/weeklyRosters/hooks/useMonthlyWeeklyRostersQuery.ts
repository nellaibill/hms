import type { MonthlyWeeklyRosterQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';
import { getMockWeeklyRostersForMonth } from '../mockWeeklyRostersStore';

export const monthlyWeeklyRostersQueryKey = (query: MonthlyWeeklyRosterQuery) => ['weeklyRosters', 'monthly', query] as const;

export function useMonthlyWeeklyRostersQuery(query: MonthlyWeeklyRosterQuery) {
  return useQuery({
    queryKey: monthlyWeeklyRostersQueryKey(query),
    queryFn: async () => {
      try {
        const result = await weeklyRostersApi.getMonthlyWeeklyRosters(query);
        return { ...result, source: 'live' as const };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...getMockWeeklyRostersForMonth(query), source: 'mock' as const };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
