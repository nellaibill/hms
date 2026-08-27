import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { CreateConversationRequest } from '@hms/shared';
import { conversationsApi } from '@/services/apiClient';
import { conversationsQueryKey } from './useConversationsQuery';
import { messagesQueryKey } from './useMessagesQuery';

export function useCreateConversationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateConversationRequest) => conversationsApi.create(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: conversationsQueryKey }),
  });
}

export function useSendMessageMutation(conversationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: string) => conversationsApi.sendMessage(conversationId, { body }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: messagesQueryKey(conversationId) });
      void queryClient.invalidateQueries({ queryKey: conversationsQueryKey });
    },
  });
}

export function useMarkConversationReadMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (conversationId: string) => conversationsApi.markRead(conversationId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: conversationsQueryKey }),
  });
}
