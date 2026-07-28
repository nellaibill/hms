import { useNavigate } from 'react-router-dom';
import { LogOut, Moon, Sun, UserRound } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { useAuth } from '@/features/auth/AuthContext';
import { roleDefinitions } from '@/features/auth/mockUsers';
import { useTheme } from '@/lib/theme-provider';

function initialsOf(name: string) {
  return name
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();
}

export function ProfileMenu() {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();

  const roleLabel = roleDefinitions.find((definition) => definition.id === user?.role)?.label ?? user?.role;

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger asChild>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="User Login / Profile Details">
              <Avatar className="h-8 w-8">
                <AvatarFallback>{user ? initialsOf(user.name) : <UserRound className="h-4 w-4" />}</AvatarFallback>
              </Avatar>
            </Button>
          </DropdownMenuTrigger>
        </TooltipTrigger>
        <TooltipContent>User Login / Profile Details</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel>
          <span className="block text-sm font-medium">{user?.name}</span>
          <span className="block text-xs font-normal text-muted-foreground">{roleLabel}</span>
          <span className="block text-xs font-normal text-muted-foreground">{user?.department}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem>My Profile</DropdownMenuItem>
        <DropdownMenuItem>Preferences</DropdownMenuItem>
        <DropdownMenuItem onSelect={(event) => { event.preventDefault(); toggleTheme(); }}>
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          Switch to {theme === 'dark' ? 'light' : 'dark'} theme
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={handleLogout} className="text-destructive focus:text-destructive">
          <LogOut className="h-4 w-4" />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
