import { AlertTriangle, Bell, Info, OctagonAlert } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { dashboardNotifications, type DashboardNotification } from '../mockData';

const severityMeta: Record<DashboardNotification['severity'], { icon: typeof Info; className: string }> = {
  info: { icon: Info, className: 'bg-info/10 text-info' },
  warning: { icon: AlertTriangle, className: 'bg-warning/10 text-warning' },
  critical: { icon: OctagonAlert, className: 'bg-destructive/10 text-destructive' },
};

export function NotificationsCard() {
  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-warning/10 text-warning">
          <Bell className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Notifications</CardTitle>
          <CardDescription className="mt-0.5">{dashboardNotifications.length} unread</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col divide-y divide-border pt-0">
        {dashboardNotifications.map((notification) => {
          const meta = severityMeta[notification.severity];
          const Icon = meta.icon;
          return (
            <div key={notification.id} className="flex items-start gap-3 py-2.5 first:pt-0 last:pb-0">
              <span className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-full', meta.className)}>
                <Icon className="h-3.5 w-3.5" />
              </span>
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-foreground">{notification.title}</p>
                <p className="truncate text-xs text-muted-foreground">{notification.detail}</p>
                <p className="mt-0.5 text-[11px] text-muted-foreground/70">{notification.time}</p>
              </div>
            </div>
          );
        })}
      </CardContent>
    </Card>
  );
}
