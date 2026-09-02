import { useMutation, useQueryClient } from '@tanstack/react-query';
import { collectSample } from '../apiLaboratoryRepository';
import type { CollectSampleRequest } from '../types';

export function useCollectSampleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ itemId, request }: { itemId: string; request: CollectSampleRequest }) => collectSample(itemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
