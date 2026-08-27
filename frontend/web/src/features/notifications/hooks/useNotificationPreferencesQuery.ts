import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UpdateNotificationPreferenceRequest } from '@hms/shared';
import { notificationPreferencesApi } from '@/services/apiClient';

export const notificationPreferencesQueryKey = ['notification-preferences', 'mine'] as const;

export function useNotificationPreferencesQuery() {
  return useQuery({
    queryKey: notificationPreferencesQueryKey,
    queryFn: () => notificationPreferencesApi.getMine(),
  });
}

export function useUpsertNotificationPreferenceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateNotificationPreferenceRequest) => notificationPreferencesApi.upsertMine(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationPreferencesQueryKey }),
  });
}
