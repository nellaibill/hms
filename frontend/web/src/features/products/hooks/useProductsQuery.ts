import type { PagedProducts, ProductListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';
import { listMockProducts } from '../mockProductsStore';

export const productsQueryKey = (query: ProductListQuery) => ['products', 'list', query] as const;

/** Result carries `source` so the UI can flag when it's showing offline demo data. */
export type PagedProductsResult = PagedProducts & { source: 'live' | 'mock' };

export function useProductsQuery(query: ProductListQuery) {
  return useQuery({
    queryKey: productsQueryKey(query),
    queryFn: async (): Promise<PagedProductsResult> => {
      try {
        const result = await productsApi.getProducts(query);
        return { ...result, source: 'live' };
      } catch (err) {
        // Only fall back when the backend is genuinely unreachable — a real ApiError
        // (backend up, request rejected) should still surface normally.
        if (err instanceof NetworkError) {
          return { ...listMockProducts(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
