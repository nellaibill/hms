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
    changePassword: '/api/platform/auth/change-password',
  },
  /** Mirrors HMS.Modules.Platform.Endpoints.HospitalsController. */
  /** Mirrors HMS.Modules.Backups.Endpoints.PlatformBackupsController. */
  platformBackups: {
    base: '/api/platform/backups',
    download: (key: string) => `/api/platform/backups/${key}/download`,
  },
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
    migrate: (id: string) => `/api/platform/hospitals/${id}/migrate`,
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
    report: '/api/v1/patients/report',
    reportSummary: '/api/v1/patients/report/summary',
    reportExport: '/api/v1/patients/report/export',
  },
  /** Cross-patient visits list, for Patient Reports — mirrors PatientVisitsController's
   * absolute-route GetAll action (deliberately not nested under a patientId). */
  patientVisits: {
    all: '/api/v1/patient-visits',
    /** GET ?visitType=OP&months=6 — visits per month, for the Executive Dashboard (DASH-01). */
    monthlyCounts: '/api/v1/patient-visits/monthly-counts',
  },
  /** Bulk patient import (Super Admin only) — mirrors HMS.Modules.Patients.Endpoints.PatientImportController. */
  patientImport: {
    base: '/api/v1/patients/import',
    template: '/api/v1/patients/import/template',
    byId: (batchId: string) => `/api/v1/patients/import/${batchId}`,
    rows: (batchId: string) => `/api/v1/patients/import/${batchId}/rows`,
    report: (batchId: string) => `/api/v1/patients/import/${batchId}/report`,
    commit: (batchId: string) => `/api/v1/patients/import/${batchId}/commit`,
  },
  branding: {
    base: '/api/v1/branding',
    logo: '/api/v1/branding/logo',
  },
  /** Mirrors HMS.Modules.Backups.Endpoints.TenantBackupsController. */
  backups: {
    mine: '/api/v1/backups/mine',
    mineDownload: '/api/v1/backups/mine/download',
  },
  /** Mirrors HMS.Modules.Documents.Endpoints.DocumentsController — see documentsApi.ts. */
  documents: {
    base: '/api/v1/documents',
    byId: (id: string) => `/api/v1/documents/${id}`,
    content: (id: string) => `/api/v1/documents/${id}/content`,
    archive: (id: string) => `/api/v1/documents/${id}/archive`,
    summary: '/api/v1/documents/summary',
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
    diagnosis: mastersEntity('diagnoses'),
    diagnosticTest: mastersEntity('diagnostic-tests'),
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
  /**
   * Central Laboratory's typed Masters entities (DiagnosticCategory/Provider/Service/Package)
   * — kept as their own top-level section rather than folded into `masters` above, mirroring
   * dtos/diagnostics' own deliberate split from dtos/masters (bespoke UI, not the generic
   * MasterRecordDto engine), even though the URLs are still under /api/v1/masters/... since
   * that's the real backend route (HMS.Modules.Masters.Endpoints.Diagnostic*Controller).
   */
  diagnostics: {
    categories: mastersEntity('diagnostic-categories'),
    providers: mastersEntity('diagnostic-providers'),
    services: mastersEntity('diagnostic-services'),
    packages: {
      ...mastersEntity('diagnostic-packages'),
      items: (packageId: string) => `/api/v1/masters/diagnostic-packages/${packageId}/items`,
      itemById: (packageId: string, itemId: string) => `/api/v1/masters/diagnostic-packages/${packageId}/items/${itemId}`,
    },
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
      request: '/api/v1/ipd/admissions/request',
      assignBed: (id: string) => `/api/v1/ipd/admissions/${id}/assign-bed`,
      charges: (id: string) => `/api/v1/ipd/admissions/${id}/charges`,
      advances: (id: string) => `/api/v1/ipd/admissions/${id}/advances`,
      vitals: (id: string) => `/api/v1/ipd/admissions/${id}/vitals`,
      progressNotes: (id: string) => `/api/v1/ipd/admissions/${id}/progress-notes`,
      nursingAssessments: (id: string) => `/api/v1/ipd/admissions/${id}/nursing-assessments`,
      nursingNotes: (id: string) => `/api/v1/ipd/admissions/${id}/nursing-notes`,
      doctorOrders: (id: string) => `/api/v1/ipd/admissions/${id}/doctor-orders`,
      doctorOrderAdvance: (admissionId: string, orderId: string) => `/api/v1/ipd/admissions/${admissionId}/doctor-orders/${orderId}/advance`,
      doctorOrderCancel: (admissionId: string, orderId: string) => `/api/v1/ipd/admissions/${admissionId}/doctor-orders/${orderId}/cancel`,
      medicationOrders: (id: string) => `/api/v1/ipd/admissions/${id}/medication-orders`,
      medicationOrderDiscontinue: (admissionId: string, orderId: string) => `/api/v1/ipd/admissions/${admissionId}/medication-orders/${orderId}/discontinue`,
      medicationAdministrations: (admissionId: string, orderId: string) =>
        `/api/v1/ipd/admissions/${admissionId}/medication-orders/${orderId}/administrations`,
      labOrders: (id: string) => `/api/v1/ipd/admissions/${id}/lab-orders`,
      finalBill: (id: string) => `/api/v1/ipd/admissions/${id}/final-bill`,
    },
    dashboard: '/api/v1/ipd/dashboard',
    /** GET ?months=6 — admissions per month, for the Executive Dashboard (DASH-01). */
    monthlyAdmissions: '/api/v1/ipd/dashboard/monthly-admissions',
  },
  /** Mirrors HMS.Modules.Billing.Endpoints.InvoicesController. */
  billing: {
    invoices: {
      base: '/api/v1/billing/invoices',
      byId: (id: string) => `/api/v1/billing/invoices/${id}`,
      recent: '/api/v1/billing/invoices/recent',
      byPatientId: (patientId: string) => `/api/v1/billing/invoices/by-patient/${patientId}`,
      recordPayment: (invoiceId: string, itemId: string) => `/api/v1/billing/invoices/${invoiceId}/items/${itemId}/payments`,
      void: (id: string) => `/api/v1/billing/invoices/${id}/void`,
      procedures: '/api/v1/billing/invoices/procedures',
      /** GET ?months=6 — revenue per month + this month by billing type (DASH-01). */
      dashboardSummary: '/api/v1/billing/invoices/dashboard-summary',
    },
  },
  /** Mirrors HMS.Modules.Radiology.Endpoints.RadiologyAiController — see radiologyApi.ts. */
  radiology: {
    aiAnalysis: (documentId: string) => `/api/v1/radiology/ai-analysis/documents/${documentId}`,
    patientAnalyses: (patientId: string) => `/api/v1/radiology/ai-analysis/patients/${patientId}`,
    review: (analysisId: string) => `/api/v1/radiology/ai-analysis/${analysisId}/review`,
  },
  /** Mirrors HMS.Modules.Laboratory.Endpoints.LabOrdersController — the lab worklist: sample
   * collection through result entry, verification, and report generation/release. Deliberately
   * has no `base` POST — orders are only ever created in-process by Billing, never via HTTP. */
  laboratory: {
    orders: {
      base: '/api/v1/laboratory/orders',
      dashboardSummary: '/api/v1/laboratory/orders/dashboard-summary',
      byId: (id: string) => `/api/v1/laboratory/orders/${id}`,
      byPatientId: (patientId: string) => `/api/v1/laboratory/orders/by-patient/${patientId}`,
      collectSample: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/collect-sample`,
      rejectSample: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/reject-sample`,
      recollect: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/recollect`,
      receive: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/receive`,
      startProcessing: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/start-processing`,
      resultDraft: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/result-draft`,
      submitForVerification: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/submit-for-verification`,
      verify: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/verify`,
      rejectForCorrection: (itemId: string) => `/api/v1/laboratory/orders/items/${itemId}/reject-for-correction`,
      generateReport: (id: string) => `/api/v1/laboratory/orders/${id}/generate-report`,
      releaseReport: (id: string) => `/api/v1/laboratory/orders/${id}/release-report`,
    },
  },
  /** Mirrors HMS.Modules.Patients.Endpoints.OpdController — the OPD Patient List's queue and
   * its consultation state transitions. */
  opd: {
    patients: '/api/v1/opd/patients',
    consultationsSummary: '/api/v1/opd/consultations/summary',
    consultationCheckIn: (id: string) => `/api/v1/opd/consultations/${id}/check-in`,
    consultationStart: (id: string) => `/api/v1/opd/consultations/${id}/start-consultation`,
    consultationComplete: (id: string) => `/api/v1/opd/consultations/${id}/complete`,
    consultationCancel: (id: string) => `/api/v1/opd/consultations/${id}/cancel`,
    consultationNoShow: (id: string) => `/api/v1/opd/consultations/${id}/no-show`,
  },
  /** Mirrors HMS.Modules.OpdConsultation.Endpoints.OpdConsultationsController — the clinical
   * note (vitals, diagnosis, investigations, plan) behind the OPD Patient List's "Consult"
   * action, keyed by consultationId (the same id `opd.consultationComplete` above operates
   * on — this is a different module's own record for that same consultation). */
  opdConsultations: {
    /** GET ?patientId= — every note already on file for one patient; never auto-creates. */
    base: '/api/v1/opd-consultations',
    byConsultationId: (consultationId: string) => `/api/v1/opd-consultations/${consultationId}`,
    saveDraft: (consultationId: string) => `/api/v1/opd-consultations/${consultationId}/draft`,
    complete: (consultationId: string) => `/api/v1/opd-consultations/${consultationId}/complete`,
    reopen: (consultationId: string) => `/api/v1/opd-consultations/${consultationId}/reopen`,
    structureNote: (consultationId: string) => `/api/v1/opd-consultations/${consultationId}/ai/structure-note`,
    /** GET ?search= (clinical-care.view) / POST (clinical-care.edit) — the consultation form's
     * own Diagnosis catalog access; the Masters endpoints require an admin permission. */
    diagnoses: '/api/v1/opd-consultations/diagnoses',
    /** GET ?department=Laboratory|Radiology — active catalog services for the investigation picker. */
    investigationServices: '/api/v1/opd-consultations/investigation-services',
    /** GET ?visitId= (finance-billing.view) — catalog-linked investigations to pre-add in OPD Billing Entry. */
    billableInvestigations: '/api/v1/opd-consultations/billable-investigations',
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
  /** Mirrors HMS.Modules.ActivityLog.Endpoints.ActivityLogsController (read-only). */
  activityLogs: {
    base: '/api/v1/activity-logs',
    byId: (id: string) => `/api/v1/activity-logs/${id}`,
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
  /**
   * Mirrors HMS.Modules.DischargeSummary.Endpoints.DischargeSummariesController. Create and
   * GetByAdmissionId deliberately share `byAdmissionId` (POST vs. GET on the same absolute
   * route override) — a discharge summary is always reached starting from a specific
   * admission for those two actions; every other action addresses the summary directly by
   * its own id.
   */
  dischargeSummaries: {
    byId: (id: string) => `/api/v1/discharge-summaries/${id}`,
    finalize: (id: string) => `/api/v1/discharge-summaries/${id}/finalize`,
    byAdmissionId: (admissionId: string) => `/api/v1/admissions/${admissionId}/discharge-summary`,
    aiDraft: (id: string) => `/api/v1/discharge-summaries/${id}/ai/draft`,
  },
} as const;
