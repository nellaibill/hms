import { History } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { recentActivities } from '../mockData';

export function RecentActivityCard() {
  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-md">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <History className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Recent Activities</CardTitle>
          <CardDescription className="mt-0.5">System-wide activity feed</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <ol className="relative flex flex-col gap-4 border-l border-border pl-4">
          {recentActivities.map((activity) => (
            <li key={activity.id} className="relative">
              <span className="absolute -left-[21px] top-1 h-2 w-2 rounded-full border-2 border-background bg-primary" />
              <p className="text-sm text-foreground">
                <span className="font-medium">{activity.actor}</span> {activity.action}
              </p>
              <p className="mt-0.5 text-xs text-muted-foreground">{activity.time}</p>
            </li>
          ))}
        </ol>
      </CardContent>
    </Card>
  );
}
