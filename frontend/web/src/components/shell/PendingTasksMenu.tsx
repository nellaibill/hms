import { ListChecks } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { pendingTasks } from '@/features/dashboard/mockData';

const PRIORITY_VARIANT = {
  High: 'destructive',
  Medium: 'warning',
  Low: 'secondary',
} as const;

/** Reuses the same pendingTasks mock data as the dashboard's Pending Tasks card, so the two stay in sync. */
export function PendingTasksMenu() {
  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="relative" aria-label="Pending Tasks">
              <ListChecks className="h-5 w-5" />
              <Badge variant="warning" className="absolute -right-1 -top-1 h-4 min-w-4 justify-center rounded-full px-1 text-[10px]">
                {pendingTasks.length}
              </Badge>
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <TooltipContent>Pending Tasks</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-80">
        <DropdownMenuLabel>Pending Tasks</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {pendingTasks.map((task) => (
          <DropdownMenuItem key={task.id} className="flex flex-col items-start gap-1 whitespace-normal">
            <span className="text-sm font-medium">{task.title}</span>
            <span className="flex w-full items-center justify-between">
              <span className="text-xs text-muted-foreground">Due {task.due}</span>
              <Badge variant={PRIORITY_VARIANT[task.priority]} className="text-[10px]">
                {task.priority}
              </Badge>
            </span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
