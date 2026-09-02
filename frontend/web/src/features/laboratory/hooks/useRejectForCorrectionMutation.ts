import { useMutation, useQueryClient } from '@tanstack/react-query';
import { rejectForCorrection } from '../apiLaboratoryRepository';
import type { RejectForCorrectionRequest } from '../types';

export function useRejectForCorrectionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ itemId, request }: { itemId: string; request: RejectForCorrectionRequest }) => rejectForCorrection(itemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
