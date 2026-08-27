import { Users } from 'lucide-react';
import type { Conversation } from '@hms/shared';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/formatRelativeTime';
import { initials } from '@/lib/initials';

interface ConversationListItemProps {
  conversation: Conversation;
  currentUserId: string;
  nameById: Map<string, string>;
  isActive: boolean;
  onClick: () => void;
}

export function ConversationListItem({ conversation, currentUserId, nameById, isActive, onClick }: ConversationListItemProps) {
  const otherParticipantIds = conversation.participantUserIds.filter((id) => id !== currentUserId);
  const displayName =
    conversation.type === 'Group'
      ? (conversation.title ?? 'Group conversation')
      : (nameById.get(otherParticipantIds[0] ?? '') ?? 'Unknown staff member');

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'flex w-full items-center gap-3 rounded-md px-3 py-2.5 text-left transition-colors hover:bg-muted/60',
        isActive && 'bg-accent',
      )}
    >
      <Avatar>
        <AvatarFallback>{conversation.type === 'Group' ? <Users className="h-4 w-4" /> : initials(displayName)}</AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-center justify-between gap-2">
          <span className={cn('truncate text-sm', conversation.unreadCount > 0 ? 'font-semibold text-foreground' : 'font-medium text-foreground')}>
            {displayName}
          </span>
          {conversation.lastMessageAt && (
            <span className="shrink-0 text-[11px] text-muted-foreground">{formatRelativeTime(conversation.lastMessageAt)}</span>
          )}
        </div>
        {conversation.type === 'Group' && (
          <span className="block truncate text-xs text-muted-foreground">{otherParticipantIds.length + 1} members</span>
        )}
      </div>
      {conversation.unreadCount > 0 && (
        <Badge variant="destructive" className="shrink-0 rounded-full px-1.5 py-0 text-[10px]">
          {conversation.unreadCount > 99 ? '99+' : conversation.unreadCount}
        </Badge>
      )}
    </button>
  );
}
