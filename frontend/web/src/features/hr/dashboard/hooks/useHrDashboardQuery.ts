import { useQuery } from '@tanstack/react-query';
import { hrDashboardApi } from '../../../../services/apiClient';

export function useHrDashboardQuery() {
  return useQuery({
    queryKey: ['hr', 'dashboard'],
    queryFn: () => hrDashboardApi.getDashboard(),
  });
}
