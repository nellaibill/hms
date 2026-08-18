import type { ModulePermissions, RoleModule } from './types';

/**
 * Mirrors the permission-catalog modules seeded in PermissionSeedData.cs (11 modules — see
 * HMS.Modules.Identity.Infrastructure.Seed.PermissionSeedData). Kept as a flat, independent
 * list rather than fetched from GET /api/v1/permissions: the catalog is static/system-seeded
 * (PermissionsController's own doc comment — "cannot be created, updated or deleted through
 * the API"), so hand-mirroring it here carries the same low drift risk as the identical
 * module-string duplication in config/navigation.ts.
 *
 * 'identity-administration' was missing from this list — every other module a role could be
 * scoped to was here, but not the one gating Roles/Users/Masters/Settings themselves, so no
 * custom (non-Super-Admin) role could ever be granted it through this UI.
 */
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
  { id: 'identity-administration', label: 'Roles, Users & Settings' },
];

/** Every module defaulted to no access — the starting point for a new role. */
export function buildEmptyPermissions(): Record<string, ModulePermissions> {
  const result: Record<string, ModulePermissions> = {};
  for (const module of ROLE_MODULES) {
    result[module.id] = { view: false, create: false, edit: false, delete: false };
  }
  return result;
}
