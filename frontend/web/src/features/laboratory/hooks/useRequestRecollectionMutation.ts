import { useMutation, useQueryClient } from '@tanstack/react-query';
import { requestRecollection } from '../apiLaboratoryRepository';

export function useRequestRecollectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => requestRecollection(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
