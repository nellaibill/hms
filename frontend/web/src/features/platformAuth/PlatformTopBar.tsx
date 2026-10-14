import { Building2, DatabaseBackup, LogOut, ShieldCheck } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/shell/ThemeToggle';
import { cn } from '@/lib/utils';
import { usePlatformAuth } from './PlatformAuthContext';
import { PLATFORM_CONTAINER } from './platformLayout';

/** The Platform Portal's sticky top bar: brand (back to the hospital list), theme, Backups,
 * Security, the signed-in operator and Sign out. */
export function PlatformTopBar() {
  const { user, logout } = usePlatformAuth();
  const navigate = useNavigate();

  const initials = (user?.fullName ?? user?.email ?? '?')
    .split(/\s+/)
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();

  return (
    <header className="sticky top-0 z-20 border-b border-border bg-background/95 backdrop-blur">
      <div className={cn(PLATFORM_CONTAINER, 'flex items-center justify-between gap-3 py-3')}>
        <Link to="/platform/dashboard" className="flex items-center gap-3 rounded-md">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-sm">
            <Building2 className="h-5 w-5" />
          </span>
          <div className="leading-tight">
            <p className="font-semibold text-foreground">Platform Portal</p>
            <p className="hidden text-xs text-muted-foreground sm:block">
              Hospitals, features and backups
            </p>
          </div>
        </Link>
        <div className="flex items-center gap-1.5">
          <ThemeToggle />
          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5"
            onClick={() => navigate('/platform/backups')}
          >
            <DatabaseBackup className="h-4 w-4" />
            <span className="hidden sm:inline">Backups</span>
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5"
            onClick={() => navigate('/platform/security')}
          >
            <ShieldCheck className="h-4 w-4" />
            <span className="hidden sm:inline">Security</span>
          </Button>
          <span className="mx-1 hidden h-6 w-px bg-border sm:block" aria-hidden />
          {user && (
            <div className="hidden items-center gap-2 pr-1 md:flex" title={user.email}>
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                {initials}
              </span>
              <span className="max-w-[10rem] truncate text-sm font-medium text-foreground">
                {user.fullName ?? user.email}
              </span>
            </div>
          )}
          <Button variant="outline" size="sm" className="gap-1.5" onClick={logout}>
            <LogOut className="h-4 w-4" />
            <span className="hidden sm:inline">Sign out</span>
          </Button>
        </div>
      </div>
    </header>
  );
}
