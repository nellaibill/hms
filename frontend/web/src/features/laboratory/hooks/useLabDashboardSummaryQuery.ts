import { useQuery } from '@tanstack/react-query';
import { getLabDashboardSummary } from '../apiLaboratoryRepository';

export const labDashboardSummaryQueryKey = ['labOrders', 'dashboard-summary'] as const;

export function useLabDashboardSummaryQuery() {
  return useQuery({
    queryKey: labDashboardSummaryQueryKey,
    queryFn: () => getLabDashboardSummary(),
  });
}
