import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '../layouts/AppLayout';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { RequireRole } from '../features/auth/RequireRole';
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
const BrandingSettingsPage = lazy(() => import('../pages/settings/BrandingSettingsPage'));
const MastersHubPage = lazy(() => import('../pages/masters/MastersHubPage'));
const MasterListPage = lazy(() => import('../pages/masters/MasterListPage'));
const MasterFormPage = lazy(() => import('../pages/masters/MasterFormPage'));
const ProductsListPage = lazy(() => import('../pages/products/ProductsListPage'));
const ProductCreatePage = lazy(() => import('../pages/products/ProductCreatePage'));
const ProductViewPage = lazy(() => import('../pages/products/ProductViewPage'));
const ProductEditPage = lazy(() => import('../pages/products/ProductEditPage'));
const InvoiceLedgerPage = lazy(() => import('../pages/finance/InvoiceLedgerPage'));
const InvoiceDetailPage = lazy(() => import('../pages/finance/InvoiceDetailPage'));
const InvoiceCreatePage = lazy(() => import('../pages/finance/InvoiceCreatePage'));
const IncomeExpenseReportPage = lazy(() => import('../pages/finance/IncomeExpenseReportPage'));
const HrHubPage = lazy(() => import('../pages/hr/HrHubPage'));
const ShiftsListPage = lazy(() => import('../pages/hr/ShiftsListPage'));
const ShiftCreatePage = lazy(() => import('../pages/hr/ShiftCreatePage'));
const ShiftViewPage = lazy(() => import('../pages/hr/ShiftViewPage'));
const ShiftEditPage = lazy(() => import('../pages/hr/ShiftEditPage'));
const StaffAvailabilityListPage = lazy(() => import('../pages/hr/StaffAvailabilityListPage'));
const StaffAvailabilityCreatePage = lazy(() => import('../pages/hr/StaffAvailabilityCreatePage'));
const StaffAvailabilityViewPage = lazy(() => import('../pages/hr/StaffAvailabilityViewPage'));
const StaffAvailabilityEditPage = lazy(() => import('../pages/hr/StaffAvailabilityEditPage'));
const WeeklyRostersListPage = lazy(() => import('../pages/hr/WeeklyRostersListPage'));
const WeeklyRosterCreatePage = lazy(() => import('../pages/hr/WeeklyRosterCreatePage'));
const WeeklyRosterViewPage = lazy(() => import('../pages/hr/WeeklyRosterViewPage'));
const WeeklyRosterEditPage = lazy(() => import('../pages/hr/WeeklyRosterEditPage'));
const ShiftAssignmentsListPage = lazy(() => import('../pages/hr/ShiftAssignmentsListPage'));
const ShiftAssignmentCreatePage = lazy(() => import('../pages/hr/ShiftAssignmentCreatePage'));
const ShiftAssignmentViewPage = lazy(() => import('../pages/hr/ShiftAssignmentViewPage'));
const ShiftAssignmentEditPage = lazy(() => import('../pages/hr/ShiftAssignmentEditPage'));
const ShiftSwapRequestsListPage = lazy(() => import('../pages/hr/ShiftSwapRequestsListPage'));
const ShiftSwapRequestCreatePage = lazy(() => import('../pages/hr/ShiftSwapRequestCreatePage'));
const ShiftSwapRequestViewPage = lazy(() => import('../pages/hr/ShiftSwapRequestViewPage'));
const ShiftSwapRequestEditPage = lazy(() => import('../pages/hr/ShiftSwapRequestEditPage'));
const MonthlyRosterCalendarPage = lazy(() => import('../pages/hr/MonthlyRosterCalendarPage'));

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
  '/support/inventory': withSuspense(<ProductsListPage />),
  '/finance/accounts': withSuspense(<InvoiceLedgerPage />),
  '/admin/hr': withSuspense(<HrHubPage />),
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

// Roles Management — reachable from the Settings page ("Roles & Permissions"), mirroring
// how Users is wired in above. Has a real HMS.Modules.Identity backend (apiRoleRepository.ts
// falls back to mockRolesStore.ts only when that backend is unreachable).
const roleRoutes = [
  { path: 'admin/roles', element: withSuspense(<RolesListPage />) },
  { path: 'admin/roles/new', element: withSuspense(<RoleFormPage mode="create" />) },
  { path: 'admin/roles/:id', element: withSuspense(<RoleFormPage mode="view" />) },
  { path: 'admin/roles/:id/edit', element: withSuspense(<RoleFormPage mode="edit" />) },
];

// Theme & Branding — reachable from the Settings page ("System Configuration"),
// route-gated to admin/superAdmin via RequireRole since ProtectedRoute alone
// only checks authentication, not role.
const brandingRoutes = [
  {
    element: <RequireRole roles={['admin', 'superAdmin']} />,
    children: [{ path: 'admin/settings/branding', element: withSuspense(<BrandingSettingsPage />) }],
  },
];

// Masters (Reference Data) — reachable from the Settings page ("Master Data"). One generic
// list/form pair driven by an :entityKey route param covers all ~16 entities from
// docs/03_Masters_ERD instead of per-entity page code.
const mastersRoutes = [
  { path: 'admin/masters', element: withSuspense(<MastersHubPage />) },
  { path: 'admin/masters/:entityKey', element: withSuspense(<MasterListPage />) },
  { path: 'admin/masters/:entityKey/new', element: withSuspense(<MasterFormPage mode="create" />) },
  { path: 'admin/masters/:entityKey/:id', element: withSuspense(<MasterFormPage mode="view" />) },
  { path: 'admin/masters/:entityKey/:id/edit', element: withSuspense(<MasterFormPage mode="edit" />) },
];

// Products (HMS.Modules.Products) — core Product CRUD, reachable from the "Hospital
// Inventory Management" nav leaf ('support/inventory', wired via specialPages above). Sub-
// resources (batches, barcodes, images, prices, tax mappings) are a future pass — see
// ProductDetails.tsx's "coming soon" section. Mirrors userRoutes' shape.
const productRoutes = [
  { path: 'support/inventory/new', element: withSuspense(<ProductCreatePage />) },
  { path: 'support/inventory/:id', element: withSuspense(<ProductViewPage />) },
  { path: 'support/inventory/:id/edit', element: withSuspense(<ProductEditPage />) },
];

// Finance & Billing (UI-only, mock data — no backend module yet, mirrors Roles Management).
// The landing ledger ('finance/accounts') is already wired via specialPages above; this
// covers the detail sub-route, mirroring userRoutes'/roleRoutes' shape.
const financeRoutes = [
  { path: 'finance/accounts/new', element: withSuspense(<InvoiceCreatePage />) },
  { path: 'finance/accounts/reports', element: withSuspense(<IncomeExpenseReportPage />) },
  { path: 'finance/accounts/:id', element: withSuspense(<InvoiceDetailPage />) },
];

// Human Resource Management — Duty Roster (HMS.Modules.HR), reachable from the '/admin/hr'
// nav leaf (wired via specialPages above). Route-gated via RequireRole since the nav-level
// role filter alone doesn't block direct URL access. Includes superAdmin alongside the nav
// leaf's own ['hr','admin'] — filterNavigationForRole (config/navigation.ts) already treats
// superAdmin/admin as seeing every leaf regardless of its roles list, so the route guard
// must honor that same bypass (mirrors brandingRoutes' ['admin','superAdmin']), or a
// superAdmin who clicks the visible sidebar item gets redirected away.
const hrRoutes = [
  {
    element: <RequireRole roles={['hr', 'admin', 'superAdmin']} />,
    children: [
      { path: 'admin/hr/shifts', element: withSuspense(<ShiftsListPage />) },
      { path: 'admin/hr/shifts/new', element: withSuspense(<ShiftCreatePage />) },
      { path: 'admin/hr/shifts/:id', element: withSuspense(<ShiftViewPage />) },
      { path: 'admin/hr/shifts/:id/edit', element: withSuspense(<ShiftEditPage />) },
      { path: 'admin/hr/staff-availability', element: withSuspense(<StaffAvailabilityListPage />) },
      { path: 'admin/hr/staff-availability/new', element: withSuspense(<StaffAvailabilityCreatePage />) },
      { path: 'admin/hr/staff-availability/:id', element: withSuspense(<StaffAvailabilityViewPage />) },
      { path: 'admin/hr/staff-availability/:id/edit', element: withSuspense(<StaffAvailabilityEditPage />) },
      { path: 'admin/hr/weekly-rosters', element: withSuspense(<WeeklyRostersListPage />) },
      { path: 'admin/hr/weekly-rosters/new', element: withSuspense(<WeeklyRosterCreatePage />) },
      { path: 'admin/hr/weekly-rosters/:id', element: withSuspense(<WeeklyRosterViewPage />) },
      { path: 'admin/hr/weekly-rosters/:id/edit', element: withSuspense(<WeeklyRosterEditPage />) },
      { path: 'admin/hr/shift-assignments', element: withSuspense(<ShiftAssignmentsListPage />) },
      { path: 'admin/hr/shift-assignments/new', element: withSuspense(<ShiftAssignmentCreatePage />) },
      { path: 'admin/hr/shift-assignments/:id', element: withSuspense(<ShiftAssignmentViewPage />) },
      { path: 'admin/hr/shift-assignments/:id/edit', element: withSuspense(<ShiftAssignmentEditPage />) },
      { path: 'admin/hr/shift-swap-requests', element: withSuspense(<ShiftSwapRequestsListPage />) },
      { path: 'admin/hr/shift-swap-requests/new', element: withSuspense(<ShiftSwapRequestCreatePage />) },
      { path: 'admin/hr/shift-swap-requests/:id', element: withSuspense(<ShiftSwapRequestViewPage />) },
      { path: 'admin/hr/shift-swap-requests/:id/edit', element: withSuspense(<ShiftSwapRequestEditPage />) },
      { path: 'admin/hr/monthly-calendar', element: withSuspense(<MonthlyRosterCalendarPage />) },
    ],
  },
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
            ...brandingRoutes,
            ...mastersRoutes,
            ...productRoutes,
            ...financeRoutes,
            ...hrRoutes,
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
