import { ListChecks } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { pendingTasks, type PendingTask } from '../mockData';

const priorityVariant: Record<PendingTask['priority'], 'destructive' | 'warning' | 'secondary'> = {
  High: 'destructive',
  Medium: 'warning',
  Low: 'secondary',
};

export function PendingTasksCard() {
  return (
    <Card className="flex h-full flex-col transition-shadow hover:shadow-soft-md">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <ListChecks className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Pending Tasks</CardTitle>
          <CardDescription className="mt-0.5">{pendingTasks.length} awaiting action</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col divide-y divide-border pt-0">
        {pendingTasks.map((task) => (
          <div key={task.id} className="flex items-start justify-between gap-3 py-2.5 first:pt-0 last:pb-0">
            <div className="min-w-0">
              <p className="text-sm font-medium leading-snug text-foreground">{task.title}</p>
              <p className="mt-0.5 text-xs text-muted-foreground">Due {task.due}</p>
            </div>
            <Badge variant={priorityVariant[task.priority]} className="shrink-0 text-[10px]">
              {task.priority}
            </Badge>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
