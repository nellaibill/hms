import { useQuery } from '@tanstack/react-query';
import { conversationsApi } from '@/services/apiClient';

// Same polling-only reasoning as frontend/web/src/features/notifications/hooks/
// useNotificationsQuery.ts — no websocket/SignalR exists in this codebase.
const POLL_INTERVAL_MS = 15_000;

export const conversationsQueryKey = ['conversations', 'list'] as const;

export function useConversationsQuery() {
  return useQuery({
    queryKey: conversationsQueryKey,
    queryFn: () => conversationsApi.getMine(),
    refetchInterval: POLL_INTERVAL_MS,
  });
}
