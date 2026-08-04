import { useQuery } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';

export function useWeeklyRosterQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['weeklyRosters', 'detail', id],
    queryFn: () => weeklyRostersApi.getWeeklyRosterById(id as string),
    enabled: Boolean(id),
  });
}
