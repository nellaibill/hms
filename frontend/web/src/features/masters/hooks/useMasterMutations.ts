import { useMutation, useQueryClient } from '@tanstack/react-query';
import { getMasterStore } from '../engine/registry';

function useInvalidateMasters(entityKey: string) {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['masters', entityKey] });
}

export function useCreateMasterMutation(entityKey: string) {
  const invalidate = useInvalidateMasters(entityKey);
  return useMutation({
    mutationFn: async (values: Record<string, unknown>) => {
      const store = getMasterStore(entityKey);
      if (!store) throw new Error(`Unknown Masters entity "${entityKey}".`);
      return store.create(values);
    },
    onSuccess: invalidate,
  });
}

export function useUpdateMasterMutation(entityKey: string) {
  const invalidate = useInvalidateMasters(entityKey);
  return useMutation({
    mutationFn: async ({ id, values }: { id: string; values: Record<string, unknown> }) => {
      const store = getMasterStore(entityKey);
      if (!store) throw new Error(`Unknown Masters entity "${entityKey}".`);
      return store.update(id, values);
    },
    onSuccess: invalidate,
  });
}
