import type { LabOrderItemResponse, LabOrderListQuery, LabOrderResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { laboratoryApi } from '@/services/apiClient';

export interface OpdInvestigationRow {
  order: LabOrderResponse;
  item: LabOrderItemResponse;
}

export const opdInvestigationsQueryKey = (query: LabOrderListQuery) => ['opd', 'investigations', 'list', query] as const;

/** OPD Investigations List tab — every lab/radiology order item placed from OPD (source
 * 'OP'), flattened to one row per item (an order with 3 tests renders as 3 rows). */
export function useOpdInvestigationsQuery(query: LabOrderListQuery) {
  return useQuery({
    queryKey: opdInvestigationsQueryKey(query),
    queryFn: async () => {
      const paged = await laboratoryApi.getOrders({ ...query, source: 'OP' });
      const rows: OpdInvestigationRow[] = paged.items.flatMap((order) => order.items.map((item) => ({ order, item })));
      return { rows, meta: paged.meta };
    },
    placeholderData: (previous) => previous,
  });
}
