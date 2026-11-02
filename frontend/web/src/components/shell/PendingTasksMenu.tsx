import { ListChecks } from 'lucide-react';
import { Fragment } from 'react';
import { useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { HeaderIconLabel } from '@/components/shell/HeaderIconLabel';
import { useApprovalTasks } from '@/features/billing';
import { formatRelativeTime } from '@/lib/formatRelativeTime';

/** The approvals waiting on the signed-in user (useApprovalTasks) — each opens where it's
 * decided. The badge counts every waiting request, not just the ones listed. */
export function PendingTasksMenu() {
  const navigate = useNavigate();
  const { groups, total } = useApprovalTasks();
  const visibleGroups = groups.filter((group) => group.total > 0);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-auto w-auto px-2.5 py-1" aria-label="Pending Tasks">
          <HeaderIconLabel
            icon={ListChecks}
            label="Tasks"
            showChevron
            badge={
              total > 0 && (
                <Badge
                  variant="warning"
                  className="absolute right-0 top-0 flex h-5 min-w-5 -translate-y-1/2 translate-x-1/2 items-center justify-center rounded-full px-1 text-[11px] font-semibold leading-none ring-2 ring-header"
                >
                  {total > 99 ? '99+' : total}
                </Badge>
              )
            }
          />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-96">
        <DropdownMenuLabel>Pending Tasks</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {visibleGroups.length === 0 ? (
          <p className="px-2 py-6 text-center text-sm text-muted-foreground">Nothing is waiting for your approval.</p>
        ) : (
          <div className="max-h-[28rem] overflow-y-auto overflow-x-hidden">
            {visibleGroups.map((group, index) => (
              <Fragment key={group.key}>
                {index > 0 && <DropdownMenuSeparator />}
                <div className="flex items-center justify-between px-2 pb-1 pt-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  <span>{group.label}</span>
                  <span className="normal-case tracking-normal">{group.total} waiting</span>
                </div>
                {group.tasks.map((task) => (
                  <DropdownMenuItem key={task.id} className="flex flex-col items-start gap-0.5 whitespace-normal" onSelect={() => navigate(task.to)}>
                    <span className="text-sm font-medium">{task.title}</span>
                    <span className="flex w-full items-start justify-between gap-2 text-xs text-muted-foreground">
                      <span className="line-clamp-2">{task.detail}</span>
                      <span className="shrink-0">{formatRelativeTime(task.requestedAt)}</span>
                    </span>
                  </DropdownMenuItem>
                ))}
                <DropdownMenuItem className="justify-center text-xs font-medium text-primary" onSelect={() => navigate(group.viewAllTo)}>
                  {group.total > group.tasks.length ? `View all ${group.total}` : 'Open the approval list'}
                </DropdownMenuItem>
              </Fragment>
            ))}
          </div>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
