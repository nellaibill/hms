import { useQuery } from '@tanstack/react-query';
import { mastersApi } from '../../../../services/apiClient';

export interface ProductCategoryOption {
  id: string;
  categoryName: string;
}

export const pharmacyProductCategoriesQueryKey = ['pharmacy', 'product-categories', 'list'] as const;

/**
 * Backs the Dispense screen's Quick Pick category tabs — calls mastersApi directly (real
 * API only) rather than the generic masters store, same reasoning as this folder's
 * useProductsQuery: Pharmacy must never fall back to demo data.
 */
export function useProductCategoriesQuery() {
  return useQuery({
    queryKey: pharmacyProductCategoriesQueryKey,
    queryFn: async () => {
      const result = await mastersApi.list('productCategory', { pageSize: 100, isActive: true, sort: 'categoryName' });
      return result.items.map((record): ProductCategoryOption => ({
        id: String(record.id),
        categoryName: String(record.categoryName ?? ''),
      }));
    },
  });
}
