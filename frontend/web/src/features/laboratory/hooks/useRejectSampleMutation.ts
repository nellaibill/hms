import { useMutation, useQueryClient } from '@tanstack/react-query';
import { rejectSample } from '../apiLaboratoryRepository';
import type { RejectSampleRequest } from '../types';

export function useRejectSampleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ itemId, request }: { itemId: string; request: RejectSampleRequest }) => rejectSample(itemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
