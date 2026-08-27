import { useQuery } from '@tanstack/react-query';
import { notificationsApi } from '@/services/apiClient';

// No websocket/SignalR exists anywhere in this codebase (frontend/FrontendArchitecture.md
// §6) — periodic polling via React Query's refetchInterval is the only established
// live-update mechanism, same as every other "did something change on the server" need.
const POLL_INTERVAL_MS = 30_000;

export const notificationsQueryKey = (isRead?: boolean) => ['notifications', 'list', { isRead }] as const;

export function useNotificationsQuery(isRead?: boolean) {
  return useQuery({
    queryKey: notificationsQueryKey(isRead),
    queryFn: () => notificationsApi.getMine({ isRead, pageSize: 50 }),
    refetchInterval: POLL_INTERVAL_MS,
  });
}

export const unreadCountQueryKey = ['notifications', 'unread-count'] as const;

export function useUnreadCountQuery() {
  return useQuery({
    queryKey: unreadCountQueryKey,
    queryFn: () => notificationsApi.getUnreadCount(),
    refetchInterval: POLL_INTERVAL_MS,
  });
}
