import { Bell } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { mockNotifications } from '@/components/shell/mockNotifications';

export function NotificationsMenu() {
  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="relative" aria-label="Notifications">
              <Bell className="h-5 w-5" />
              <Badge
                variant="destructive"
                className="absolute right-0 top-0 flex h-5 min-w-5 -translate-y-1/2 translate-x-1/2 items-center justify-center rounded-full px-1 text-[11px] font-semibold leading-none ring-2 ring-header"
              >
                {mockNotifications.length}
              </Badge>
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <TooltipContent>Notifications</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-80">
        <DropdownMenuLabel>Notifications</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {mockNotifications.map((notification) => (
          <DropdownMenuItem key={notification.id} className="flex flex-col items-start gap-0.5 whitespace-normal">
            <span className="text-sm font-medium">{notification.title}</span>
            <span className="text-xs text-muted-foreground">{notification.detail}</span>
            <span className="text-[11px] text-muted-foreground">{notification.time}</span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
