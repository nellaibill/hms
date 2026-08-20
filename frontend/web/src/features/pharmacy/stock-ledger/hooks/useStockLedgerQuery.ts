import type { StockLedgerListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export const stockLedgerQueryKey = (query: StockLedgerListQuery) => ['pharmacy', 'stock-ledger', 'list', query] as const;

export function useStockLedgerQuery(query: StockLedgerListQuery) {
  return useQuery({
    queryKey: stockLedgerQueryKey(query),
    queryFn: () => pharmacyApi.getStockLedger(query),
    placeholderData: (previous) => previous,
  });
}
