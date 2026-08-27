import { MessageSquarePlus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { ConversationListItem } from './ConversationListItem';
import { useConversationsQuery } from '../hooks/useConversationsQuery';
import { useStaffNameMap } from '../hooks/useStaffNameMap';

interface ConversationListProps {
  activeConversationId: string | null;
  onSelect: (conversationId: string) => void;
  onNewConversation: () => void;
}

export function ConversationList({ activeConversationId, onSelect, onNewConversation }: ConversationListProps) {
  const { user } = useAuth();
  const conversationsQuery = useConversationsQuery();
  const { nameById } = useStaffNameMap();

  const conversations = conversationsQuery.data ?? [];

  return (
    <div className="flex h-full min-h-0 w-full flex-col border-r border-border sm:w-80">
      <div className="flex items-center justify-between gap-2 border-b border-border px-3 py-2.5">
        <h2 className="text-sm font-semibold text-foreground">Conversations</h2>
        <Button variant="ghost" size="icon" aria-label="Start a new conversation" onClick={onNewConversation}>
          <MessageSquarePlus className="h-4 w-4" />
        </Button>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto p-1.5">
        {conversationsQuery.isPending ? (
          <p className="px-3 py-6 text-center text-sm text-muted-foreground">Loading conversations…</p>
        ) : conversations.length === 0 ? (
          <div className="flex flex-col items-center gap-2 px-4 py-10 text-center">
            <p className="text-sm text-muted-foreground">No conversations yet.</p>
            <Button variant="outline" size="sm" onClick={onNewConversation}>
              Start a conversation
            </Button>
          </div>
        ) : (
          conversations.map((conversation) => (
            <ConversationListItem
              key={conversation.id}
              conversation={conversation}
              currentUserId={user?.id ?? ''}
              nameById={nameById}
              isActive={conversation.id === activeConversationId}
              onClick={() => onSelect(conversation.id)}
            />
          ))
        )}
      </div>
    </div>
  );
}
