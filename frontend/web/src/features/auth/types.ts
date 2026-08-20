export type Role =
  | 'superAdmin'
  | 'admin'
  | 'receptionist'
  | 'doctor'
  | 'nurse'
  | 'labTechnician'
  | 'radiologist'
  | 'pharmacist'
  | 'hr'
  | 'accounts';

export interface RoleDefinition {
  id: Role;
  label: string;
  description: string;
}

/** The signed-in principal, derived from HMS.Modules.Identity.Contracts.LoginUserResponse. */
export interface AuthUser {
  id: string;
  name: string;
  /** The Login Type selected at sign-in (validated server-side to match the user's assigned role). */
  role: Role;
  username: string;
  email: string;
  /** The freeform Role name from the Roles module (e.g. "Doctor / Consultant") — display only. */
  roleName: string;
  /** Permission keys (e.g. "patient-management.create") attached to the user's role — same
   * set the backend checks via [RequirePermission(...)]. UI-only hinting: the backend is the
   * real enforcement boundary, this just avoids showing actions the user can't complete. */
  permissionKeys: string[];
  /** FeatureCatalog keys enabled for this tenant (Tenant Feature/Module Management) — which
   * schema-level modules the hospital has at all, independent of this user's own permissions.
   * UI-only hinting like permissionKeys: the backend's FeatureAuthorizationHandler checks
   * live tenant state, never this snapshot. */
  featureKeys: string[];
  /** True when this user's current password was set by someone else (admin reset, or the
   * initial password chosen during hospital registration) — mirrors
   * HMS.Modules.Identity.Contracts.LoginUserResponse.MustChangePassword. ProtectedRoute
   * redirects to /change-password until this clears. */
  mustChangePassword: boolean;
}
