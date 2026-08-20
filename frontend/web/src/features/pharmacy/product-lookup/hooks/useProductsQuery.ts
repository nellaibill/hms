import type { ProductListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../../services/apiClient';

export const pharmacyProductsQueryKey = (query: ProductListQuery) => ['pharmacy', 'products', 'list', query] as const;

/**
 * Active-product lookup backing the Product picker on the Stock Receipt / Dispense
 * forms — a dedicated hook (rather than reusing features/products' useProductsQuery) since
 * that one falls back to demo data when the backend is unreachable, which Pharmacy must
 * never do (real API integration only).
 */
export function useProductsQuery(query: ProductListQuery) {
  return useQuery({
    queryKey: pharmacyProductsQueryKey(query),
    queryFn: () => productsApi.getProducts(query),
    placeholderData: (previous) => previous,
  });
}
