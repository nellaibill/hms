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

/**
 * Mirrors HMS.Modules.Platform.Contracts.PlatformLoginResponse — either a completed login
 * (mfaRequired false, token/user populated) or the first half of a two-step MFA login
 * (mfaRequired true, mfaChallengeToken populated, token/user absent).
 */
export interface PlatformLoginResponse {
  mfaRequired: boolean;
  mfaChallengeToken?: string | null;
  token?: string | null;
  expiresIn: number;
  user?: PlatformLoginUserResponse | null;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformMfaVerifyRequest. */
export interface PlatformMfaVerifyRequest {
  challengeToken: string;
  code: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformMfaSetupResponse. */
export interface PlatformMfaSetupResponse {
  secret: string;
  otpAuthUri: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformMfaEnableRequest. */
export interface PlatformMfaEnableRequest {
  code: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformMfaDisableRequest. */
export interface PlatformMfaDisableRequest {
  code: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformMfaStatusResponse. */
export interface PlatformMfaStatusResponse {
  enabled: boolean;
}

/** Mirrors HMS.Modules.Platform.Contracts.PlatformChangePasswordRequest. */
export interface PlatformChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
