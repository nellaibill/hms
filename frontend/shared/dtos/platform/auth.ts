/** Mirrors HMS.Modules.Platform.Contracts.PlatformLoginRequest. */
export interface PlatformLoginRequest {
  email: string;
  password: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformLoginUserResponse. */
export interface PlatformLoginUserResponse {
  id: string;
  fullName: string;
  email: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformLoginResponse. */
export interface PlatformLoginResponse {
  token: string;
  expiresIn: number;
  user: PlatformLoginUserResponse;
}
