import { useMutation, useQueryClient } from '@tanstack/react-query';
import { receiveSample } from '../apiLaboratoryRepository';

export function useReceiveSampleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => receiveSample(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
