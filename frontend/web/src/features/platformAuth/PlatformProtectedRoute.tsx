import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { usePlatformAuth } from './PlatformAuthContext';

export function PlatformProtectedRoute() {
  const { isAuthenticated } = usePlatformAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/platform/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
