import type { StockReceiptListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export const stockReceiptsQueryKey = (query: StockReceiptListQuery) => ['pharmacy', 'stock-receipts', 'list', query] as const;

export function useStockReceiptsQuery(query: StockReceiptListQuery) {
  return useQuery({
    queryKey: stockReceiptsQueryKey(query),
    queryFn: () => pharmacyApi.getStockReceipts(query),
    placeholderData: (previous) => previous,
  });
}
