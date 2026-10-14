import type { StockSummaryListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

// Keyed under 'stock-balances' for the same reason as useAvailableBatchesQuery.
export const stockSummaryQueryKey = (query: StockSummaryListQuery) => ['pharmacy', 'stock-balances', 'summary', query] as const;

export function useStockSummaryQuery(query: StockSummaryListQuery) {
  return useQuery({
    queryKey: stockSummaryQueryKey(query),
    queryFn: () => pharmacyApi.getStockSummary(query),
    placeholderData: (previous) => previous,
  });
}
