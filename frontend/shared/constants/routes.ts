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
    changePassword: '/api/v1/auth/change-password',
  },
  /** Mirrors HMS.Modules.Platform.Endpoints.PlatformAuthController — entirely separate from the hospital `auth` routes above. */
  platformAuth: {
    login: '/api/platform/auth/login',
    me: '/api/platform/auth/me',
    logout: '/api/platform/auth/logout',
    mfaVerify: '/api/platform/auth/mfa/verify',
    mfaStatus: '/api/platform/auth/mfa/status',
    mfaSetup: '/api/platform/auth/mfa/setup',
    mfaEnable: '/api/platform/auth/mfa/enable',
    mfaDisable: '/api/platform/auth/mfa/disable',
  },
  /** Mirrors HMS.Modules.Platform.Endpoints.HospitalsController. */
  platformHospitals: {
    base: '/api/platform/hospitals',
    stats: '/api/platform/hospitals/stats',
    status: (id: string) => `/api/platform/hospitals/${id}/status`,
    deleted: '/api/platform/hospitals/deleted',
    deletePreview: (id: string) => `/api/platform/hospitals/${id}/delete-preview`,
    byId: (id: string) => `/api/platform/hospitals/${id}`,
    restore: (id: string) => `/api/platform/hospitals/${id}/restore`,
    configuration: (id: string) => `/api/platform/hospitals/${id}/configuration`,
    features: (id: string) => `/api/platform/hospitals/${id}/features`,
  },
  users: {
    base: '/api/v1/users',
    byId: (id: string) => `/api/v1/users/${id}`,
    activate: (id: string) => `/api/v1/users/${id}/activate`,
    deactivate: (id: string) => `/api/v1/users/${id}/deactivate`,
    password: (id: string) => `/api/v1/users/${id}/password`,
    profilePhoto: (id: string) => `/api/v1/users/${id}/profile-photo`,
    directory: '/api/v1/users/directory',
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
    allergies: (id: string) => `/api/v1/patients/${id}/allergies`,
    allergyById: (id: string, allergyId: string) => `/api/v1/patients/${id}/allergies/${allergyId}`,
    visits: (id: string) => `/api/v1/patients/${id}/visits`,
    visitById: (id: string, visitId: string) => `/api/v1/patients/${id}/visits/${visitId}`,
  },
  branding: {
    base: '/api/v1/branding',
    logo: '/api/v1/branding/logo',
  },
  /** Mirrors HMS.Modules.Documents.Endpoints.DocumentsController — see documentsApi.ts. */
  documents: {
    base: '/api/v1/documents',
    byId: (id: string) => `/api/v1/documents/${id}`,
    content: (id: string) => `/api/v1/documents/${id}/content`,
  },
  /**
   * Masters (Reference Data) — mirrors HMS.Modules.Masters.Endpoints.*Controller. Keyed by
   * the same camelCase entityKey used throughout frontend/web/src/features/masters, mapped
   * to each controller's kebab-plural route segment.
   */
  masters: {
    appointmentType: mastersEntity('appointment-types'),
    brand: mastersEntity('brands'),
    consultant: mastersEntity('consultants'),
    consultationType: mastersEntity('consultation-types'),
    currency: mastersEntity('currencies'),
    customer: mastersEntity('customers'),
    department: mastersEntity('departments'),
    designation: mastersEntity('designations'),
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
    /** Mirrors HMS.Modules.Products.Endpoints.ProductBatchesController. */
    batches: (productId: string) => `/api/v1/products/${productId}/batches`,
    batchById: (productId: string, id: string) => `/api/v1/products/${productId}/batches/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.ShiftsController. */
  shifts: {
    base: '/api/v1/shifts',
    byId: (id: string) => `/api/v1/shifts/${id}`,
  },
  /**
   * Mirrors HMS.Modules.Masters.Endpoints.DepartmentsController — Department was
   * consolidated into Masters (see docs/DecisionLog.md); this dedicated typed client
   * (DepartmentsApi/DepartmentSelect) is kept for the HR/Calendar forms that reference it,
   * pointed at the same route the generic masters.department entry above also serves.
   */
  departments: {
    base: '/api/v1/masters/departments',
    byId: (id: string) => `/api/v1/masters/departments/${id}`,
  },
  /**
   * Mirrors HMS.Modules.Masters.Endpoints.StatesController — read-only, no admin CRUD, so
   * this doesn't live under `masters` above (that block is generic CRUD-shaped, driven by
   * MastersApi/MastersEntityKey — see mastersApi.ts). India is the only supported country,
   * so states are the top level (no Country route).
   */
  states: {
    base: '/api/v1/masters/states',
    districts: (stateId: string) => `/api/v1/masters/states/${stateId}/districts`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.StaffAvailabilityController (singular route segment, per the backend's own doc comment). */
  staffAvailability: {
    base: '/api/v1/staff-availability',
    byId: (id: string) => `/api/v1/staff-availability/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.WeeklyRostersController. */
  weeklyRosters: {
    base: '/api/v1/weekly-rosters',
    byId: (id: string) => `/api/v1/weekly-rosters/${id}`,
    publish: (id: string) => `/api/v1/weekly-rosters/${id}/publish`,
    copy: (id: string) => `/api/v1/weekly-rosters/${id}/copy`,
    monthly: '/api/v1/weekly-rosters/monthly',
  },
  /** Mirrors HMS.Modules.HR.Endpoints.ShiftAssignmentsController. */
  shiftAssignments: {
    base: '/api/v1/shift-assignments',
    byId: (id: string) => `/api/v1/shift-assignments/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.ShiftSwapRequestsController. */
  shiftSwapRequests: {
    base: '/api/v1/shift-swap-requests',
    byId: (id: string) => `/api/v1/shift-swap-requests/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.EmployeesController — Hospital HR Management MVP. */
  employees: {
    base: '/api/v1/employees',
    byId: (id: string) => `/api/v1/employees/${id}`,
    activate: (id: string) => `/api/v1/employees/${id}/activate`,
    deactivate: (id: string) => `/api/v1/employees/${id}/deactivate`,
    leaveBalances: (id: string) => `/api/v1/employees/${id}/leave-balances`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.AttendanceController — Hospital HR Management MVP. */
  attendance: {
    base: '/api/v1/attendance',
    byId: (id: string) => `/api/v1/attendance/${id}`,
    checkIn: '/api/v1/attendance/check-in',
    checkOut: '/api/v1/attendance/check-out',
  },
  /** Mirrors HMS.Modules.HR.Endpoints.LeaveTypesController — Hospital HR Management MVP. */
  leaveTypes: {
    base: '/api/v1/leave-types',
    byId: (id: string) => `/api/v1/leave-types/${id}`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.LeaveRequestsController — Hospital HR Management MVP. */
  leaveRequests: {
    base: '/api/v1/leave-requests',
    byId: (id: string) => `/api/v1/leave-requests/${id}`,
    approve: (id: string) => `/api/v1/leave-requests/${id}/approve`,
    reject: (id: string) => `/api/v1/leave-requests/${id}/reject`,
    cancel: (id: string) => `/api/v1/leave-requests/${id}/cancel`,
  },
  /** Mirrors HMS.Modules.HR.Endpoints.HrDashboardController — Hospital HR Management MVP. */
  hrDashboard: {
    base: '/api/v1/hr/dashboard',
  },
  /** Mirrors HMS.Modules.Calendar.Endpoints.EventsController. */
  events: {
    base: '/api/v1/events',
    byId: (id: string) => `/api/v1/events/${id}`,
    month: '/api/v1/events/month',
    bulk: '/api/v1/events/bulk',
  },
  /** Mirrors HMS.Modules.IPD.Endpoints.*Controller. */
  ipd: {
    wards: {
      base: '/api/v1/ipd/wards',
      byId: (id: string) => `/api/v1/ipd/wards/${id}`,
    },
    beds: {
      base: '/api/v1/ipd/beds',
      byId: (id: string) => `/api/v1/ipd/beds/${id}`,
      available: '/api/v1/ipd/beds/available',
    },
    admissions: {
      base: '/api/v1/ipd/admissions',
      byId: (id: string) => `/api/v1/ipd/admissions/${id}`,
      transferBed: (id: string) => `/api/v1/ipd/admissions/${id}/transfer-bed`,
      transferHistory: (id: string) => `/api/v1/ipd/admissions/${id}/transfer-history`,
      bedHistory: (id: string) => `/api/v1/ipd/admissions/${id}/bed-history`,
      discharge: (id: string) => `/api/v1/ipd/admissions/${id}/discharge`,
      charges: (id: string) => `/api/v1/ipd/admissions/${id}/charges`,
    },
    dashboard: '/api/v1/ipd/dashboard',
  },
  /** Mirrors HMS.Modules.Billing.Endpoints.InvoicesController. */
  billing: {
    invoices: {
      base: '/api/v1/billing/invoices',
      byId: (id: string) => `/api/v1/billing/invoices/${id}`,
      byPatientId: (patientId: string) => `/api/v1/billing/invoices/by-patient/${patientId}`,
      recordPayment: (invoiceId: string, itemId: string) => `/api/v1/billing/invoices/${invoiceId}/items/${itemId}/payments`,
    },
  },
  /** Mirrors HMS.Modules.Pharmacy.Endpoints.*Controller — no PUT/DELETE anywhere, every list is append-only history. */
  pharmacy: {
    stockReceipts: {
      base: '/api/v1/pharmacy/stock-receipts',
      byId: (id: string) => `/api/v1/pharmacy/stock-receipts/${id}`,
    },
    dispenses: {
      base: '/api/v1/pharmacy/dispenses',
      byId: (id: string) => `/api/v1/pharmacy/dispenses/${id}`,
      cart: '/api/v1/pharmacy/dispenses/cart',
    },
    stockBalances: {
      base: '/api/v1/pharmacy/stock-balances',
      byProductBatch: (productId: string, productBatchId: string) => `/api/v1/pharmacy/stock-balances/${productId}/${productBatchId}`,
    },
    stockLedger: '/api/v1/pharmacy/stock-ledger',
  },
  /** Mirrors HMS.Modules.Notifications.Endpoints.*Controller. */
  notifications: {
    base: '/api/v1/notifications',
    unreadCount: '/api/v1/notifications/unread-count',
    markRead: (id: string) => `/api/v1/notifications/${id}/read`,
    markAllRead: '/api/v1/notifications/read-all',
  },
  notificationPreferences: '/api/v1/notification-preferences',
  notificationTemplates: {
    base: '/api/v1/notification-templates',
    byId: (id: string) => `/api/v1/notification-templates/${id}`,
  },
  /** Mirrors HMS.Modules.Messaging.Endpoints.ConversationsController. */
  conversations: {
    base: '/api/v1/conversations',
    messages: (id: string) => `/api/v1/conversations/${id}/messages`,
    read: (id: string) => `/api/v1/conversations/${id}/read`,
  },
} as const;
