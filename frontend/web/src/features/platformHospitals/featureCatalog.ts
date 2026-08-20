/**
 * Display labels for FeatureCatalog keys (HMS.Shared.Kernel.FeatureCatalog) — the "which
 * modules does this hospital have" set, distinct from ROLE_MODULES (the RBAC permission-
 * category set). The actual key list/mandatory subset always comes from the API response
 * (TenantFeaturesResponse.allFeatures/mandatoryFeatures) — this map only supplies a human-
 * readable label per key, same reasoning as ROLE_MODULES's own doc comment.
 */
export const FEATURE_LABELS: Record<string, string> = {
  identity: 'Identity & Users',
  masters: 'Master Data',
  patients: 'Patients',
  documents: 'Documents',
  branding: 'Branding',
  hr: 'HR & Staffing',
  calendar: 'Calendar',
  products: 'Products & Inventory',
  ipd: 'IPD (In-Patient Department)',
  // UI-only — no real backend module/schema behind these yet (mirrors
  // HMS.Shared.Kernel.FeatureCatalog.UiOnly). Toggling these only controls sidebar/route
  // visibility; there's nothing server-side to provision or enforce.
  opd: 'Out Patient Department (OPD)',
  ot: 'Operation Theatre (OT)',
  pharmacy: 'Pharmacy',
  'central-laboratory': 'Central Laboratory',
  radiology: 'Radiology',
  'blood-bank': 'Blood Bank',
  ambulance: 'Ambulance',
  finance: 'Accounts and Finance',
  'records-and-certificates': 'Records and Certificates',
  'activity-log': 'Activity Log',
  'messages-and-notifications': 'Messages and Notifications',
  reports: 'Reports',
  'e-mrd': 'E-MRD',
};

export function featureLabel(key: string): string {
  return FEATURE_LABELS[key] ?? key;
}

/** Mirrors HMS.Shared.Kernel.FeatureCatalog.Optional — the only keys a platform admin can
 * choose at hospital-creation time (mandatory ones are always included server-side). */
export const OPTIONAL_FEATURE_KEYS = [
  'hr',
  'calendar',
  'products',
  'ipd',
  'opd',
  'ot',
  'pharmacy',
  'central-laboratory',
  'radiology',
  'blood-bank',
  'ambulance',
  'finance',
  'records-and-certificates',
  'activity-log',
  'messages-and-notifications',
  'reports',
  'e-mrd',
];
