import { useMutation, useQueryClient } from '@tanstack/react-query';
import { startProcessing } from '../apiLaboratoryRepository';

export function useStartProcessingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => startProcessing(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
