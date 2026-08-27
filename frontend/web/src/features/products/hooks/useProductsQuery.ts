import type { PagedProducts, ProductListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';

export const productsQueryKey = (query: ProductListQuery) => ['products', 'list', query] as const;

export function useProductsQuery(query: ProductListQuery) {
  return useQuery({
    queryKey: productsQueryKey(query),
    queryFn: (): Promise<PagedProducts> => productsApi.getProducts(query),
    placeholderData: (previous) => previous,
  });
}
