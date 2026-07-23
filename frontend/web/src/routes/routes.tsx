import { lazy } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { AppLayout } from '../layouts/AppLayout';

// Route-level code splitting — each page is its own chunk (docs/FrontendArchitecture.md §4).
const UsersListPage = lazy(() => import('../pages/users/UsersListPage'));
const UserCreatePage = lazy(() => import('../pages/users/UserCreatePage'));
const UserEditPage = lazy(() => import('../pages/users/UserEditPage'));
const UserViewPage = lazy(() => import('../pages/users/UserViewPage'));

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <UsersListPage /> },
      { path: 'users', element: <UsersListPage /> },
      { path: 'users/new', element: <UserCreatePage /> },
      { path: 'users/:id', element: <UserViewPage /> },
      { path: 'users/:id/edit', element: <UserEditPage /> },
    ],
  },
]);
