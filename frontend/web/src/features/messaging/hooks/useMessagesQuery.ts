import { useQuery } from '@tanstack/react-query';
import { conversationsApi } from '@/services/apiClient';

const POLL_INTERVAL_MS = 8_000;

export const messagesQueryKey = (conversationId: string) => ['conversations', conversationId, 'messages'] as const;

/** Polls the currently-open conversation's history — a tighter interval than the
 * conversation list (frontend/web/src/features/messaging/hooks/useConversationsQuery.ts)
 * since an open thread is what the user is actively watching. */
export function useMessagesQuery(conversationId: string | null) {
  return useQuery({
    queryKey: messagesQueryKey(conversationId ?? ''),
    queryFn: () => conversationsApi.getMessages(conversationId!, 1, 50),
    enabled: conversationId !== null,
    refetchInterval: conversationId !== null ? POLL_INTERVAL_MS : false,
  });
}
