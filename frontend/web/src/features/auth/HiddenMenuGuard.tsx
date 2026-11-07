import type { ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { EyeOff } from 'lucide-react';
import { findHiddenMenuForPath } from '@/config/navigation';
import { useAuth } from './AuthContext';

/**
 * Blocks the pages of a menu turned off for the signed-in user's role (ADR-128), so a direct
 * address or an old bookmark can't open what the sidebar hides. UI-only, like
 * RequirePermissionRoute: the role keeps its permissions, because other screens need the data
 * behind them (OPD's lists, Lab and Radiology billing).
 */
export function HiddenMenuGuard({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const { pathname } = useLocation();
  const hiddenMenu = findHiddenMenuForPath(pathname, user?.hiddenMenus ?? []);

  if (hiddenMenu) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <EyeOff className="h-8 w-8 text-muted-foreground" />
        <p className="font-medium text-foreground">{hiddenMenu.label} is turned off for your role.</p>
        <p className="text-sm text-muted-foreground">Ask your hospital administrator if you need it.</p>
      </div>
    );
  }

  return <>{children}</>;
}
