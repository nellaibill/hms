import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '../layouts/AppLayout';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { PlaceholderPage } from '../pages/PlaceholderPage';
import { getAllLeaves } from '../config/navigation';

// Route-level code splitting (docs/FrontendArchitecture.md §4).
const LoginPage = lazy(() => import('../pages/auth/LoginPage'));
const DashboardPage = lazy(() => import('../pages/dashboard/DashboardPage'));
const SettingsPage = lazy(() => import('../pages/settings/SettingsPage'));
const UsersListPage = lazy(() => import('../pages/users/UsersListPage'));
const UserCreatePage = lazy(() => import('../pages/users/UserCreatePage'));
const UserViewPage = lazy(() => import('../pages/users/UserViewPage'));
const UserEditPage = lazy(() => import('../pages/users/UserEditPage'));
const PatientRegistrationHubPage = lazy(() => import('../pages/patients/PatientRegistrationHubPage'));
const PatientsListPage = lazy(() => import('../pages/patients/PatientsListPage'));
const PatientRegistrationCreatePage = lazy(() => import('../pages/patients/PatientRegistrationCreatePage'));
const PatientViewPage = lazy(() => import('../pages/patients/PatientViewPage'));
const PatientEditPage = lazy(() => import('../pages/patients/PatientEditPage'));
const RolesListPage = lazy(() => import('../pages/roles/RolesListPage'));
const RoleFormPage = lazy(() => import('../pages/roles/RoleFormPage'));

const shellFallback = (
  <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Loading…</div>
);

const withSuspense = (element: React.ReactNode) => <Suspense fallback={shellFallback}>{element}</Suspense>;

// Dashboard, Settings, Reception & Registration, and Patient Enquiry are the
// nav leaves with a real page — every other leaf renders the shared
// PlaceholderPage generated from the nav config, keeping that config the
// single source of truth for the sidebar.
const specialPages: Record<string, React.ReactNode> = {
  '/dashboard': withSuspense(<DashboardPage />),
  '/admin/settings': withSuspense(<SettingsPage />),
  '/patients/registration': withSuspense(<PatientRegistrationHubPage />),
  '/patients/enquiry': withSuspense(<PatientsListPage />),
};

const moduleRoutes = getAllLeaves().map((leaf) => ({
  path: leaf.path.slice(1),
  element: specialPages[leaf.path] ?? <PlaceholderPage title={leaf.label} description={leaf.description} icon={leaf.icon} />,
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

// Patients (HMS.Modules.Patients) — New Patient Registration, the MVP core-form slice of
// docs/PatientRegistrationModule.md (see docs/DecisionLog.md for what's deferred). The
// landing hub ('patients/registration') and 'patients/enquiry' are already wired via
// specialPages above; these cover the create/view/edit sub-routes, mirroring userRoutes' shape.
const patientRoutes = [
  { path: 'patients/registration/new', element: withSuspense(<PatientRegistrationCreatePage />) },
  { path: 'patients/registration/:id', element: withSuspense(<PatientViewPage />) },
  { path: 'patients/registration/:id/edit', element: withSuspense(<PatientEditPage />) },
];

// Roles Management (UI-only, mock data — no backend module yet) — reachable from the
// Settings page ("Roles & Permissions"), mirroring how Users is wired in above.
const roleRoutes = [
  { path: 'admin/roles', element: withSuspense(<RolesListPage />) },
  { path: 'admin/roles/new', element: withSuspense(<RoleFormPage mode="create" />) },
  { path: 'admin/roles/:id', element: withSuspense(<RoleFormPage mode="view" />) },
  { path: 'admin/roles/:id/edit', element: withSuspense(<RoleFormPage mode="edit" />) },
];

export const router = createBrowserRouter(
  [
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
            ...patientRoutes,
            ...roleRoutes,
          ],
        },
      ],
    },
    {
      path: '*',
      element: <Navigate to="/dashboard" replace />,
    },
  ],
  // Matches Vite's configured `base` (see vite.config.ts) — '/' locally, '/<repo>/' on
  // GitHub Pages — so route paths don't need the subpath baked into every href.
  { basename: import.meta.env.BASE_URL },
);
