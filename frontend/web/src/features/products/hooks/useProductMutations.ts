import type { CreateProductRequest, UpdateProductRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';
import { createMockProduct, updateMockProduct } from '../mockProductsStore';

function useInvalidateProducts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['products'] });
}

export function useCreateProductMutation() {
  const invalidateProducts = useInvalidateProducts();
  return useMutation({
    mutationFn: async (request: CreateProductRequest) => {
      try {
        return await productsApi.createProduct(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockProduct(request);
        }
        throw err;
      }
    },
    onSuccess: invalidateProducts,
  });
}

export function useUpdateProductMutation() {
  const invalidateProducts = useInvalidateProducts();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateProductRequest }) => {
      try {
        return await productsApi.updateProduct(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockProduct(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidateProducts,
  });
}
