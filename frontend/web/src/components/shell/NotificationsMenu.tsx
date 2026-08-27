import { Bell } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { NotificationsList, useUnreadCountQuery } from '@/features/notifications';

export function NotificationsMenu() {
  const navigate = useNavigate();
  const unreadCountQuery = useUnreadCountQuery();
  const unreadCount = unreadCountQuery.data ?? 0;

  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="relative" aria-label="Notifications">
              <Bell className="h-5 w-5" />
              {unreadCount > 0 && (
                <Badge
                  variant="destructive"
                  className="absolute right-0 top-0 flex h-5 min-w-5 -translate-y-1/2 translate-x-1/2 items-center justify-center rounded-full px-1 text-[11px] font-semibold leading-none ring-2 ring-header"
                >
                  {unreadCount > 99 ? '99+' : unreadCount}
                </Badge>
              )}
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <TooltipContent>Notifications</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-96">
        <DropdownMenuLabel>Notifications</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <div className="max-h-96 overflow-y-auto px-1 py-1">
          <NotificationsList compact />
        </div>
        <DropdownMenuSeparator />
        <Button variant="ghost" size="sm" className="w-full" onClick={() => navigate('/engagement/messages')}>
          View all
        </Button>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
