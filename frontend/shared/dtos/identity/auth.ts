/** Mirrors HMS.Modules.Identity.Contracts.LoginRequest. The hospital itself is identified
 * by the X-Hospital-Code header (see AuthApi.login), not a body field. */
export interface LoginRequest {
  loginType: string;
  username: string;
  password: string;
}

/** Mirrors HMS.Modules.Identity.Contracts.LoginUserResponse. */
export interface LoginUserResponse {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  roleId: string;
  roleName: string;
  loginType: string;
  profilePhotoUrl?: string | null;
  permissionKeys: string[];
  /** FeatureCatalog keys enabled for this tenant (Tenant Feature/Module Management) — UI/
   * nav-gating convenience only, a login-time snapshot. The backend never trusts this for
   * authorization; it always checks live tenant state instead. */
  featureKeys: string[];
  mustChangePassword: boolean;
}

/** Mirrors HMS.Modules.Identity.Contracts.LoginResponse. */
export interface LoginResponse {
  token: string;
  expiresIn: number;
  user: LoginUserResponse;
}

/** Mirrors HMS.Modules.Identity.Contracts.ChangePasswordRequest. */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
