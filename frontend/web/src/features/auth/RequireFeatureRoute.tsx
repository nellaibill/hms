import { Outlet } from 'react-router-dom';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from './AuthContext';

interface RequireFeatureRouteProps {
  feature: string;
}

/**
 * Route-level feature gate (Outlet-based, for wrapping a route-config `children` array) —
 * the feature-flag sibling of RequirePermissionRoute.tsx. Renders an inline "not available"
 * message in place instead of a silent redirect, so a direct URL visit is diagnosable. Nest
 * alongside RequirePermissionRoute where a route needs both checks — React Router renders
 * nested `element`s in order, so both guards run. Frontend-only hinting: the backend's
 * [RequireFeature(key)] remains the real enforcement boundary.
 */
export function RequireFeatureRoute({ feature }: RequireFeatureRouteProps) {
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

  return <Outlet />;
}
