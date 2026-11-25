import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@/features/auth/AuthContext';
import { notificationsApi } from '@/services/apiClient';

/** The API serves notifications only while the hospital has Messages & Notifications on; with it
 * off every call is a 403, which the header used to repeat every 30 s on every page. */
const NOTIFICATIONS_FEATURE = 'messages-and-notifications';

// No websocket/SignalR exists anywhere in this codebase (frontend/FrontendArchitecture.md
// §6) — periodic polling via React Query's refetchInterval is the only established
// live-update mechanism, same as every other "did something change on the server" need.
const POLL_INTERVAL_MS = 30_000;

export const notificationsQueryKey = (isRead?: boolean) => ['notifications', 'list', { isRead }] as const;

export function useNotificationsQuery(isRead?: boolean) {
  const { hasFeature } = useAuth();
  return useQuery({
    queryKey: notificationsQueryKey(isRead),
    queryFn: () => notificationsApi.getMine({ isRead, pageSize: 50 }),
    refetchInterval: POLL_INTERVAL_MS,
    enabled: hasFeature(NOTIFICATIONS_FEATURE),
  });
}

export const unreadCountQueryKey = ['notifications', 'unread-count'] as const;

export function useUnreadCountQuery() {
  const { hasFeature } = useAuth();
  return useQuery({
    queryKey: unreadCountQueryKey,
    queryFn: () => notificationsApi.getUnreadCount(),
    refetchInterval: POLL_INTERVAL_MS,
    enabled: hasFeature(NOTIFICATIONS_FEATURE),
  });
}
