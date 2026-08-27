import type { Message } from '@hms/shared';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/formatRelativeTime';

interface MessageBubbleProps {
  message: Message;
  isMine: boolean;
  senderName: string;
  showSenderName: boolean;
}

export function MessageBubble({ message, isMine, senderName, showSenderName }: MessageBubbleProps) {
  return (
    <div className={cn('flex flex-col', isMine ? 'items-end' : 'items-start')}>
      {showSenderName && !isMine && <span className="mb-0.5 px-1 text-xs font-medium text-muted-foreground">{senderName}</span>}
      <div
        className={cn(
          'max-w-[75%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-sm shadow-sm',
          isMine ? 'rounded-br-sm bg-primary text-primary-foreground' : 'rounded-bl-sm bg-muted text-foreground',
        )}
      >
        {message.body}
      </div>
      <span className="mt-0.5 px-1 text-[11px] text-muted-foreground">{formatRelativeTime(message.createdAt)}</span>
    </div>
  );
}
