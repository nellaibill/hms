import { Outlet } from 'react-router-dom';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from './AuthContext';

interface RequirePermissionRouteProps {
  permission: string;
}

/**
 * Route-level permission gate (React Router `element`+`Outlet` shape, for wrapping a
 * `children` array in routes.tsx) — the Outlet-based sibling of RequirePermission.tsx's
 * children-wrapper shape, for module route groups (e.g. HR, IPD) that previously used
 * RequireRole. Renders an inline Access Denied message in place instead of a silent
 * redirect, so a direct URL visit is diagnosable rather than looking like navigation
 * failed. Frontend-only hinting: the backend's [RequirePermission(key)] remains the
 * real enforcement boundary.
 */
export function RequirePermissionRoute({ permission }: RequirePermissionRouteProps) {
  const { hasPermission } = useAuth();

  if (!hasPermission(permission)) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <ShieldAlert className="h-8 w-8 text-destructive" />
        <p className="font-medium text-foreground">You don't have permission to access this.</p>
        <p className="text-sm text-muted-foreground">Contact an administrator if you believe this is a mistake.</p>
      </div>
    );
  }

  return <Outlet />;
}
