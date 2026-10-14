import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

// Keyed under 'stock-balances' so the receipt/dispense mutations' existing invalidation of
// that prefix refreshes what can be dispensed without either having to know about this query.
export const availableBatchesQueryKey = (productId: string | undefined, storeId: string | undefined) =>
  ['pharmacy', 'stock-balances', 'available-batches', productId, storeId ?? 'default'] as const;

/** The batches of a product that can be dispensed right now from one store (the default when
 * storeId is omitted), earliest expiry first — only enabled once a product has been chosen. */
export function useAvailableBatchesQuery(productId: string | undefined, storeId?: string) {
  return useQuery({
    queryKey: availableBatchesQueryKey(productId, storeId),
    queryFn: () => pharmacyApi.getAvailableBatches(productId as string, storeId),
    enabled: Boolean(productId),
  });
}
