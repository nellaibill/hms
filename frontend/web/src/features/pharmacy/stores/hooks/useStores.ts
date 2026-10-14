import type { CreateStoreRequest, UpdateStoreRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export const storesQueryKey = ['pharmacy', 'stores'] as const;

/** Every pharmacy store, the default first. A hospital has a handful and they rarely change,
 * so the list is fetched whole and kept for a few minutes. */
export function useStoresQuery() {
  return useQuery({
    queryKey: storesQueryKey,
    queryFn: () => pharmacyApi.getStores(),
    staleTime: 5 * 60_000,
  });
}

export function useCreateStoreMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateStoreRequest) => pharmacyApi.createStore(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: storesQueryKey }),
  });
}

export function useUpdateStoreMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateStoreRequest }) => pharmacyApi.updateStore(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: storesQueryKey }),
  });
}
