import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';

export function useProductQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['products', 'detail', id],
    queryFn: () => productsApi.getProductById(id as string),
    enabled: Boolean(id),
  });
}
