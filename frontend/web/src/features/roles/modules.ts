import type { ModulePermissions, RoleModule } from './types';

/** Mirrors the top-level domains in config/navigation.ts — kept as a flat, independent list here since Roles Management has no backend yet. */
export const ROLE_MODULES: RoleModule[] = [
  { id: 'patient-management', label: 'Patient Management' },
  { id: 'clinical-care', label: 'Clinical Care' },
  { id: 'diagnostics', label: 'Diagnostics & Ancillary' },
  { id: 'pharmacy', label: 'Pharmacy' },
  { id: 'support-services', label: 'Support Services' },
  { id: 'finance-billing', label: 'Finance & Billing' },
  { id: 'records-compliance', label: 'Records & Compliance' },
  { id: 'workforce-admin', label: 'Workforce & Administration' },
  { id: 'engagement', label: 'Engagement' },
  { id: 'reports-analytics', label: 'Reports & Analytics' },
];

/** Every module defaulted to no access — the starting point for a new role. */
export function buildEmptyPermissions(): Record<string, ModulePermissions> {
  const result: Record<string, ModulePermissions> = {};
  for (const module of ROLE_MODULES) {
    result[module.id] = { view: false, create: false, edit: false, delete: false };
  }
  return result;
}
