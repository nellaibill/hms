import type { PagedWeeklyRosters, WeeklyRosterListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';
import { listMockWeeklyRosters } from '../mockWeeklyRostersStore';

export const weeklyRostersQueryKey = (query: WeeklyRosterListQuery) => ['weeklyRosters', 'list', query] as const;

export type PagedWeeklyRostersResult = PagedWeeklyRosters & { source: 'live' | 'mock' };

export function useWeeklyRostersQuery(query: WeeklyRosterListQuery) {
  return useQuery({
    queryKey: weeklyRostersQueryKey(query),
    queryFn: async (): Promise<PagedWeeklyRostersResult> => {
      try {
        const result = await weeklyRostersApi.getWeeklyRosters(query);
        return { ...result, source: 'live' };
      } catch (err) {
        if (err instanceof NetworkError) {
          return { ...listMockWeeklyRosters(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
