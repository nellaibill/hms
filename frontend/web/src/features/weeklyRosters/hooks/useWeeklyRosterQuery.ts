import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';
import { getMockWeeklyRosterById } from '../mockWeeklyRostersStore';

export function useWeeklyRosterQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['weeklyRosters', 'detail', id],
    queryFn: async () => {
      try {
        return await weeklyRostersApi.getWeeklyRosterById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockWeeklyRosterById(id as string);
          if (mock) {
            return mock;
          }
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
