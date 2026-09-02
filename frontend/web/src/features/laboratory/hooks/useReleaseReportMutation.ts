import { useMutation, useQueryClient } from '@tanstack/react-query';
import { releaseReport } from '../apiLaboratoryRepository';

export function useReleaseReportMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (orderId: string) => releaseReport(orderId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
