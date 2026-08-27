import { AlertTriangle } from 'lucide-react';
import type { Notification } from '@hms/shared';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/formatRelativeTime';

interface NotificationRowProps {
  notification: Notification;
  onClick?: (notification: Notification) => void;
  compact?: boolean;
}

export function NotificationRow({ notification, onClick, compact }: NotificationRowProps) {
  const isEmergency = notification.severity === 'Emergency';

  return (
    <button
      type="button"
      onClick={() => onClick?.(notification)}
      className={cn(
        'flex w-full flex-col items-start gap-0.5 whitespace-normal rounded-md px-3 py-2 text-left transition-colors hover:bg-muted/60',
        !notification.isRead && 'bg-accent/40',
      )}
    >
      <span className="flex w-full items-center gap-1.5">
        {!notification.isRead && <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-primary" aria-hidden="true" />}
        {isEmergency && <AlertTriangle className="h-3.5 w-3.5 shrink-0 text-destructive" aria-hidden="true" />}
        <span className={cn('truncate text-sm', notification.isRead ? 'font-normal text-foreground' : 'font-semibold text-foreground')}>
          {notification.title}
        </span>
      </span>
      <span className={cn('text-xs text-muted-foreground', compact && 'line-clamp-2')}>{notification.body}</span>
      <span className="text-[11px] text-muted-foreground">{formatRelativeTime(notification.createdAt)}</span>
    </button>
  );
}
