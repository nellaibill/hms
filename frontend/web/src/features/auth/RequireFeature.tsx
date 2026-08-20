import type { ReactNode } from 'react';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from './AuthContext';

interface RequireFeatureProps {
  feature: string;
  children: ReactNode;
}

/**
 * Page-level guard mirroring the backend's [RequireFeature(key)] checks (see
 * HMS.Shared.Infrastructure.FeatureAuthorization) — renders an inline "not available"
 * message instead of the page content when this tenant doesn't have the module enabled, so
 * direct URL navigation can't reach a form the backend will reject on submit anyway.
 * UI-only hinting: the backend remains the real enforcement boundary, and checks live tenant
 * state rather than this login-time snapshot — see AuthUser.featureKeys's own doc comment.
 */
export function RequireFeature({ feature, children }: RequireFeatureProps) {
  const { hasFeature } = useAuth();

  if (!hasFeature(feature)) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
        <ShieldAlert className="h-8 w-8 text-destructive" />
        <p className="font-medium text-foreground">This module isn't available for your hospital.</p>
        <p className="text-sm text-muted-foreground">Contact your Platform Admin if you believe this is a mistake.</p>
      </div>
    );
  }

  return <>{children}</>;
}
