/** Mirrors HMS.Modules.Identity.Contracts.LoginRequest. */
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
}

/** Mirrors HMS.Modules.Identity.Contracts.LoginResponse. */
export interface LoginResponse {
  token: string;
  expiresIn: number;
  user: LoginUserResponse;
}
