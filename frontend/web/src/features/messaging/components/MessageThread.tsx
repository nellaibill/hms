import { useEffect, useRef, useState } from 'react';
import { Send, Users } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { initials } from '@/lib/initials';
import { useAuth } from '@/features/auth/AuthContext';
import { MessageBubble } from './MessageBubble';
import { useConversationsQuery } from '../hooks/useConversationsQuery';
import { useMarkConversationReadMutation, useSendMessageMutation } from '../hooks/useConversationMutations';
import { useMessagesQuery } from '../hooks/useMessagesQuery';
import { useStaffNameMap } from '../hooks/useStaffNameMap';

interface MessageThreadProps {
  conversationId: string;
}

export function MessageThread({ conversationId }: MessageThreadProps) {
  const { user } = useAuth();
  const currentUserId = user?.id ?? '';
  const [draft, setDraft] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);

  const conversationsQuery = useConversationsQuery();
  const messagesQuery = useMessagesQuery(conversationId);
  const { nameById } = useStaffNameMap();
  const sendMutation = useSendMessageMutation(conversationId);
  const markReadMutation = useMarkConversationReadMutation();

  const conversation = conversationsQuery.data?.find((c) => c.id === conversationId);
  const messages = messagesQuery.data?.items ?? [];
  const otherParticipantIds = (conversation?.participantUserIds ?? []).filter((id) => id !== currentUserId);
  const title =
    conversation?.type === 'Group'
      ? (conversation.title ?? 'Group conversation')
      : (nameById.get(otherParticipantIds[0] ?? '') ?? 'Conversation');

  // Marks the conversation read once, when it's opened (and again if new messages arrive
  // while it's already open — see the conversationId/messages.length dependency).
  useEffect(() => {
    if (conversation && conversation.unreadCount > 0) {
      markReadMutation.mutate(conversationId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conversationId, messages.length]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }, [messages.length]);

  function handleSend() {
    const body = draft.trim();
    if (!body) return;
    setDraft('');
    sendMutation.mutate(body);
  }

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <div className="flex items-center gap-3 border-b border-border px-4 py-2.5">
        <Avatar>
          <AvatarFallback>{conversation?.type === 'Group' ? <Users className="h-4 w-4" /> : initials(title)}</AvatarFallback>
        </Avatar>
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-foreground">{title}</p>
          {conversation?.type === 'Group' && (
            <p className="truncate text-xs text-muted-foreground">{otherParticipantIds.length + 1} members</p>
          )}
        </div>
      </div>

      <div className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto p-4">
        {messagesQuery.isPending ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Loading messages…</p>
        ) : messages.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">No messages yet — say hello.</p>
        ) : (
          messages.map((message, index) => {
            const isMine = message.senderId === currentUserId;
            const previous = messages[index - 1];
            const showSenderName = conversation?.type === 'Group' && (!previous || previous.senderId !== message.senderId);
            return (
              <MessageBubble
                key={message.id}
                message={message}
                isMine={isMine}
                senderName={nameById.get(message.senderId) ?? 'Unknown staff member'}
                showSenderName={showSenderName}
              />
            );
          })
        )}
        <div ref={bottomRef} />
      </div>

      <div className="flex items-end gap-2 border-t border-border p-3">
        <textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault();
              handleSend();
            }
          }}
          placeholder="Write a message…"
          rows={1}
          className={cn(
            'flex max-h-32 min-h-10 w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          )}
        />
        <Button size="icon" aria-label="Send message" onClick={handleSend} disabled={!draft.trim() || sendMutation.isPending}>
          <Send className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
