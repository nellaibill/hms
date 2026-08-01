import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';
import { getMockProductById } from '../mockProductsStore';

export function useProductQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['products', 'detail', id],
    queryFn: async () => {
      try {
        return await productsApi.getProductById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockProductById(id as string);
          if (mock) {
            return mock;
          }
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
