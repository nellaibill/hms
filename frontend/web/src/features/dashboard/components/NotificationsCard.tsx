import { Bell, Info, OctagonAlert } from 'lucide-react';
import type { NotificationSeverity } from '@hms/shared';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/formatRelativeTime';
import { useNotificationsQuery, useUnreadCountQuery } from '@/features/notifications';

const DASHBOARD_NOTIFICATIONS_LIMIT = 5;

const severityMeta: Record<NotificationSeverity, { icon: typeof Info; className: string }> = {
  Normal: { icon: Info, className: 'bg-info/10 text-info' },
  Emergency: { icon: OctagonAlert, className: 'bg-destructive/10 text-destructive' },
};

export function NotificationsCard() {
  const unreadCountQuery = useUnreadCountQuery();
  const notificationsQuery = useNotificationsQuery(false);
  const notifications = (notificationsQuery.data?.items ?? []).slice(0, DASHBOARD_NOTIFICATIONS_LIMIT);
  const unreadCount = unreadCountQuery.data ?? 0;

  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-warning/10 text-warning">
          <Bell className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Notifications</CardTitle>
          <CardDescription className="mt-0.5">{unreadCount} unread</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col divide-y divide-border pt-0">
        {notificationsQuery.isPending ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Loading notifications…</p>
        ) : notifications.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">You're all caught up.</p>
        ) : (
          notifications.map((notification) => {
            const meta = severityMeta[notification.severity];
            const Icon = meta.icon;
            return (
              <div key={notification.id} className="flex items-start gap-3 py-2.5 first:pt-0 last:pb-0">
                <span className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-full', meta.className)}>
                  <Icon className="h-3.5 w-3.5" />
                </span>
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-foreground">{notification.title}</p>
                  <p className="truncate text-xs text-muted-foreground">{notification.body}</p>
                  <p className="mt-0.5 text-[11px] text-muted-foreground/70">{formatRelativeTime(notification.createdAt)}</p>
                </div>
              </div>
            );
          })
        )}
      </CardContent>
    </Card>
  );
}
