import type { CreateProductRequest, UpdateProductRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '../../../services/apiClient';

function useInvalidateProducts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['products'] });
}

export function useCreateProductMutation() {
  const invalidateProducts = useInvalidateProducts();
  return useMutation({
    mutationFn: (request: CreateProductRequest) => productsApi.createProduct(request),
    onSuccess: invalidateProducts,
  });
}

export function useUpdateProductMutation() {
  const invalidateProducts = useInvalidateProducts();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateProductRequest }) => productsApi.updateProduct(id, request),
    onSuccess: invalidateProducts,
  });
}
