function mastersEntity(segment: string) {
  return {
    base: `/api/v1/masters/${segment}`,
    byId: (id: string) => `/api/v1/masters/${segment}/${id}`,
  };
}

/** API route paths for the Users module — mirrors HMS.Modules.Identity.Endpoints.UsersController. */
export const API_ROUTES = {
  auth: {
    login: '/api/v1/auth/login',
    me: '/api/v1/auth/me',
  },
  users: {
    base: '/api/v1/users',
    byId: (id: string) => `/api/v1/users/${id}`,
    activate: (id: string) => `/api/v1/users/${id}/activate`,
    deactivate: (id: string) => `/api/v1/users/${id}/deactivate`,
    password: (id: string) => `/api/v1/users/${id}/password`,
    profilePhoto: (id: string) => `/api/v1/users/${id}/profile-photo`,
  },
  roles: {
    base: '/api/v1/roles',
    byId: (id: string) => `/api/v1/roles/${id}`,
    activate: (id: string) => `/api/v1/roles/${id}/activate`,
    deactivate: (id: string) => `/api/v1/roles/${id}/deactivate`,
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
  /**
   * Masters (Reference Data) — mirrors HMS.Modules.Masters.Endpoints.*Controller. Keyed by
   * the same camelCase entityKey used throughout frontend/web/src/features/masters, mapped
   * to each controller's kebab-plural route segment.
   */
  masters: {
    brand: mastersEntity('brands'),
    currency: mastersEntity('currencies'),
    customer: mastersEntity('customers'),
    manufacturer: mastersEntity('manufacturers'),
    paymentMethod: mastersEntity('payment-methods'),
    paymentTerms: mastersEntity('payment-terms'),
    productCategory: mastersEntity('product-categories'),
    productGroup: mastersEntity('product-groups'),
    productSubCategory: mastersEntity('product-sub-categories'),
    stockAdjustmentReason: mastersEntity('stock-adjustment-reasons'),
    storageLocation: mastersEntity('storage-locations'),
    supplier: mastersEntity('suppliers'),
    tax: mastersEntity('taxes'),
    unitConversion: mastersEntity('unit-conversions'),
    unitOfMeasure: mastersEntity('units-of-measure'),
    warehouse: mastersEntity('warehouses'),
  },
  /** Mirrors HMS.Modules.Products.Endpoints.ProductsController (core Product CRUD only). */
  products: {
    base: '/api/v1/products',
    byId: (id: string) => `/api/v1/products/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.ShiftsController. */
  shifts: {
    base: '/api/v1/shifts',
    byId: (id: string) => `/api/v1/shifts/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.StaffAvailabilityController (singular route segment, per the backend's own doc comment). */
  staffAvailability: {
    base: '/api/v1/staff-availability',
    byId: (id: string) => `/api/v1/staff-availability/${id}`,
  },
} as const;
