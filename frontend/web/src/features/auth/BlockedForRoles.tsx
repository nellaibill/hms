import { ShieldAlert } from 'lucide-react';
import type { ReactNode } from 'react';
import { useAuth } from './AuthContext';
import type { Role } from './types';

interface BlockedForRolesProps {
  /** Sign-ins (login types) this page isn't for. */
  roles: readonly Role[];
  children: ReactNode;
}

/**
 * Keeps a page from sign-ins it isn't meant for, whatever their role's permissions (ADR-150):
 * a doctor's login doesn't register patients, so Reception and Registration's hub and New
 * Patient Registration are closed to it — while a patient's own page (opened from OPD or a
 * patient's name) stays open. A direct URL visit gets a clear message instead of the page.
 */
export function BlockedForRoles({ roles, children }: BlockedForRolesProps) {
  const { user } = useAuth();

  if (user && roles.includes(user.role)) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <ShieldAlert className="h-8 w-8 text-destructive" />
        <p className="font-medium text-foreground">This page isn't available for your sign-in.</p>
        <p className="text-sm text-muted-foreground">Patient registration is done at the front desk.</p>
      </div>
    );
  }

  return <>{children}</>;
}
