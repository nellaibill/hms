import type { ProductBatchListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../../services/apiClient';

export const productBatchesQueryKey = (productId: string | undefined, query: ProductBatchListQuery) =>
  ['pharmacy', 'product-batches', productId, query] as const;

/**
 * Backs the Batch picker on the Stock Receipt / Dispense forms — only enabled once a
 * product has been selected, since a batch only makes sense scoped to its parent product
 * (mirrors BedSelect's ward-scoped enabled condition).
 */
export function useProductBatchesQuery(productId: string | undefined, query: ProductBatchListQuery = {}) {
  return useQuery({
    queryKey: productBatchesQueryKey(productId, query),
    queryFn: () => productsApi.getProductBatches(productId as string, query),
    enabled: Boolean(productId),
  });
}
