import { useQuery } from '@tanstack/react-query';
import { ipdDashboardApi } from '../../../../services/apiClient';

export function useIpdDashboardQuery() {
  return useQuery({
    queryKey: ['ipd', 'dashboard'],
    queryFn: () => ipdDashboardApi.getDashboard(),
  });
}
