import { useMutation, useQueryClient } from '@tanstack/react-query';
import { submitForVerification } from '../apiLaboratoryRepository';

export function useSubmitForVerificationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => submitForVerification(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
