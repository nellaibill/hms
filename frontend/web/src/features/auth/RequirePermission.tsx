import type { ReactNode } from 'react';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from './AuthContext';

interface RequirePermissionProps {
  permission: string;
  children: ReactNode;
}

/**
 * Page-level guard mirroring the backend's [RequirePermission(key)] checks (see
 * HMS.Shared.Infrastructure.PermissionAuthorization) — renders an inline "not authorized"
 * message instead of the page content when the signed-in user's role lacks the permission,
 * so direct URL navigation can't reach a form the backend will reject on submit anyway.
 * UI-only hinting: the backend remains the real enforcement boundary.
 */
export function RequirePermission({ permission, children }: RequirePermissionProps) {
  const { hasPermission } = useAuth();

  if (!hasPermission(permission)) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <ShieldAlert className="h-8 w-8 text-destructive" />
        <p className="font-medium text-foreground">You don't have permission to do this.</p>
        <p className="text-sm text-muted-foreground">Contact an administrator if you believe this is a mistake.</p>
      </div>
    );
  }

  return <>{children}</>;
}
