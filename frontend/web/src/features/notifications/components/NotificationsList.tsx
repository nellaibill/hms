import { useState } from 'react';
import type { Notification } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { NotificationRow } from './NotificationRow';
import { useNotificationsQuery } from '../hooks/useNotificationsQuery';
import { useMarkAllNotificationsReadMutation, useMarkNotificationReadMutation } from '../hooks/useNotificationMutations';

interface NotificationsListProps {
  compact?: boolean;
  onSelect?: (notification: Notification) => void;
}

/** The notifications list, shared by the header dropdown (compact) and the full
 * notifications page. Clicking an unread row marks it read. */
export function NotificationsList({ compact, onSelect }: NotificationsListProps) {
  const [filter, setFilter] = useState<'all' | 'unread'>('all');
  const isRead = filter === 'unread' ? false : undefined;

  const notificationsQuery = useNotificationsQuery(isRead);
  const markReadMutation = useMarkNotificationReadMutation();
  const markAllReadMutation = useMarkAllNotificationsReadMutation();

  const items = notificationsQuery.data?.items ?? [];
  const visibleItems = compact ? items.slice(0, 8) : items;

  function handleSelect(notification: Notification) {
    if (!notification.isRead) {
      markReadMutation.mutate(notification.id);
    }
    onSelect?.(notification);
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <Tabs value={filter} onValueChange={(value) => setFilter(value as 'all' | 'unread')}>
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="unread">Unread</TabsTrigger>
          </TabsList>
        </Tabs>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => markAllReadMutation.mutate()}
          disabled={markAllReadMutation.isPending || items.every((item) => item.isRead)}
        >
          Mark all read
        </Button>
      </div>

      {notificationsQuery.isPending ? (
        <p className="px-3 py-6 text-center text-sm text-muted-foreground">Loading notifications…</p>
      ) : visibleItems.length === 0 ? (
        <p className="px-3 py-6 text-center text-sm text-muted-foreground">
          {filter === 'unread' ? "You're all caught up." : 'No notifications yet.'}
        </p>
      ) : (
        <div className="flex flex-col gap-0.5">
          {visibleItems.map((notification) => (
            <NotificationRow key={notification.id} notification={notification} onClick={handleSelect} compact={compact} />
          ))}
        </div>
      )}
    </div>
  );
}
