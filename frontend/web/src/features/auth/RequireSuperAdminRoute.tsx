import { ShieldAlert } from 'lucide-react';
import { Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

/**
 * Route gate for pages only the hospital's Super Admin may open (ADR-150): Settings and
 * everything under it, and the Activity Log. Checks the sign-in itself (role 'superAdmin'),
 * not a permission, so a doctor or receptionist role that was given Settings permissions for
 * another reason still can't reach them. The same Access Denied message as
 * RequirePermissionRoute, so a direct URL visit is clearly refused rather than looking broken.
 */
export function RequireSuperAdminRoute() {
  const { user } = useAuth();

  if (user?.role !== 'superAdmin') {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <ShieldAlert className="h-8 w-8 text-destructive" />
        <p className="font-medium text-foreground">Only a Super Admin can open this page.</p>
        <p className="text-sm text-muted-foreground">Sign in as the hospital's Super Admin to use Settings and the Activity Log.</p>
      </div>
    );
  }

  return <Outlet />;
}
