import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '@/services/apiClient';

/** Invalidates every "notifications" query (list + unread count) — mark-read actions
 * touch both, and the list is keyed by isRead so a targeted invalidation would need to
 * enumerate every filter combination anyway. */
function useInvalidateNotifications() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['notifications'] });
}

export function useMarkNotificationReadMutation() {
  const invalidate = useInvalidateNotifications();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: invalidate,
  });
}

export function useMarkAllNotificationsReadMutation() {
  const invalidate = useInvalidateNotifications();
  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: invalidate,
  });
}
