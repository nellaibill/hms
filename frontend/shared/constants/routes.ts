/** API route paths for the Users module — mirrors HMS.Modules.Identity.Endpoints.UsersController. */
export const API_ROUTES = {
  users: {
    base: '/api/v1/users',
    byId: (id: string) => `/api/v1/users/${id}`,
    activate: (id: string) => `/api/v1/users/${id}/activate`,
    deactivate: (id: string) => `/api/v1/users/${id}/deactivate`,
  },
} as const;
