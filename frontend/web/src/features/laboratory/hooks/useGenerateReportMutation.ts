import { useMutation, useQueryClient } from '@tanstack/react-query';
import { generateReport } from '../apiLaboratoryRepository';

export function useGenerateReportMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (orderId: string) => generateReport(orderId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
