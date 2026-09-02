import { useQuery } from '@tanstack/react-query';
import { listLabOrders } from '../apiLaboratoryRepository';
import type { LabOrderListQuery } from '../types';

export const labOrdersQueryKey = (query: LabOrderListQuery) => ['labOrders', 'list', query] as const;

/** The lab worklist — paged, searchable, filterable by status/priority/date-range. */
export function useLabOrdersQuery(query: LabOrderListQuery = {}) {
  return useQuery({
    queryKey: labOrdersQueryKey(query),
    queryFn: () => listLabOrders(query),
    placeholderData: (previous) => previous,
  });
}
