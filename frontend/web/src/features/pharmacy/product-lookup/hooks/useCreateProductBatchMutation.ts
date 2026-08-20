import type { CreateProductBatchRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '../../../../services/apiClient';

/**
 * Lets Pharmacy's Stock Receipt form create a genuinely new batch inline, since Products has
 * no batch-management screen of its own yet — without this, receiving stock into a batch
 * that doesn't already exist would be impossible through any UI in the app.
 */
export function useCreateProductBatchMutation(productId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProductBatchRequest) => productsApi.createProductBatch(productId as string, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'product-batches', productId] });
    },
  });
}
