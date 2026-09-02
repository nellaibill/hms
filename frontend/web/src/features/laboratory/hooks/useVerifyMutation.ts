import { useMutation, useQueryClient } from '@tanstack/react-query';
import { verifyResult } from '../apiLaboratoryRepository';

export function useVerifyMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => verifyResult(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
