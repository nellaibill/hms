import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import type { Role } from './types';

interface RequireRoleProps {
  roles: Role[];
}

/**
 * Route-level role gate — ProtectedRoute only checks isAuthenticated, so
 * pages that should be admin-only (e.g. Theme & Branding) wrap themselves in
 * this instead. Frontend-only, matching the app's current mock-auth posture
 * (no backend authorization exists anywhere yet).
 */
export function RequireRole({ roles }: RequireRoleProps) {
  const { user } = useAuth();

  if (!user || !roles.includes(user.role)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
