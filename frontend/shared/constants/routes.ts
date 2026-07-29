/** API route paths for the Users module — mirrors HMS.Modules.Identity.Endpoints.UsersController. */
export const API_ROUTES = {
  users: {
    base: '/api/v1/users',
    byId: (id: string) => `/api/v1/users/${id}`,
    activate: (id: string) => `/api/v1/users/${id}/activate`,
    deactivate: (id: string) => `/api/v1/users/${id}/deactivate`,
  },
  patients: {
    base: '/api/v1/patients',
    byId: (id: string) => `/api/v1/patients/${id}`,
    photo: (id: string) => `/api/v1/patients/${id}/photo`,
    idProof: (id: string) => `/api/v1/patients/${id}/id-proof`,
  },
  branding: {
    base: '/api/v1/branding',
    logo: '/api/v1/branding/logo',
  },
} as const;
