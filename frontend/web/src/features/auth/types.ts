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
}
