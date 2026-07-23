import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '../layouts/AppLayout';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { PlaceholderPage } from '../pages/PlaceholderPage';
import { getAllLeaves } from '../config/navigation';

// Route-level code splitting (docs/FrontendArchitecture.md §4).
const LoginPage = lazy(() => import('../pages/auth/LoginPage'));
const SettingsPage = lazy(() => import('../pages/settings/SettingsPage'));
const UsersListPage = lazy(() => import('../pages/users/UsersListPage'));
const UserCreatePage = lazy(() => import('../pages/users/UserCreatePage'));
const UserViewPage = lazy(() => import('../pages/users/UserViewPage'));
const UserEditPage = lazy(() => import('../pages/users/UserEditPage'));

const shellFallback = (
  <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Loading…</div>
);

const withSuspense = (element: React.ReactNode) => <Suspense fallback={shellFallback}>{element}</Suspense>;

// Settings is the one nav leaf with a real page (linking to Users, below) —
// every other leaf renders the shared PlaceholderPage generated from the nav
// config, keeping that config the single source of truth for the sidebar.
const moduleRoutes = getAllLeaves().map((leaf) => ({
  path: leaf.path.slice(1),
  element:
    leaf.path === '/admin/settings'
      ? withSuspense(<SettingsPage />)
      : <PlaceholderPage title={leaf.label} description={leaf.description} icon={leaf.icon} />,
}));

// Users (HMS.Modules.Identity) is the one reference module with a real,
// API-integrated UI — reachable from the Settings page ("User Accounts"),
// rendered inside the same AppLayout shell as every other module.
const userRoutes = [
  { path: 'users', element: withSuspense(<UsersListPage />) },
  { path: 'users/new', element: withSuspense(<UserCreatePage />) },
  { path: 'users/:id', element: withSuspense(<UserViewPage />) },
  { path: 'users/:id/edit', element: withSuspense(<UserEditPage />) },
];

export const router = createBrowserRouter([
  {
    path: '/login',
    element: withSuspense(<LoginPage />),
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          ...moduleRoutes,
          ...userRoutes,
        ],
      },
    ],
  },
  {
    path: '*',
    element: <Navigate to="/dashboard" replace />,
  },
]);
