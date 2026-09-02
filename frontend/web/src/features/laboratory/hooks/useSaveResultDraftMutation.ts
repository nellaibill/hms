import { useMutation, useQueryClient } from '@tanstack/react-query';
import { saveResultDraft } from '../apiLaboratoryRepository';
import type { SaveResultDraftRequest } from '../types';

export function useSaveResultDraftMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ itemId, request }: { itemId: string; request: SaveResultDraftRequest }) => saveResultDraft(itemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['labOrders'] }),
  });
}
