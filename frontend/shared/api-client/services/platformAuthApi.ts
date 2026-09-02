import { API_ROUTES } from '../../constants';
import type {
  PlatformChangePasswordRequest,
  PlatformLoginRequest,
  PlatformLoginResponse,
  PlatformMfaDisableRequest,
  PlatformMfaEnableRequest,
  PlatformMfaSetupResponse,
  PlatformMfaStatusResponse,
  PlatformMfaVerifyRequest,
} from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Platform module's authentication surface. Built on its own
 * HttpClient instance (frontend/web/src/services/apiClient.ts) — Platform Admins and
 * hospital users never share a token holder, same as they never share a backend database.
 */
export class PlatformAuthApi {
  constructor(private readonly client: HttpClient) {}

  async login(request: PlatformLoginRequest): Promise<PlatformLoginResponse> {
    const response = await this.client.post<PlatformLoginResponse>(API_ROUTES.platformAuth.login, request);
    return response.data;
  }

  /** Second step of a two-step MFA login — exchanges the challenge token `login` returned
   * (when `mfaRequired` is true), plus a current authenticator code, for the real token. */
  async verifyMfa(request: PlatformMfaVerifyRequest): Promise<PlatformLoginResponse> {
    const response = await this.client.post<PlatformLoginResponse>(API_ROUTES.platformAuth.mfaVerify, request);
    return response.data;
  }

  /** Whether the currently-authenticated Platform Admin's account has MFA enabled. */
  async getMfaStatus(): Promise<PlatformMfaStatusResponse> {
    const response = await this.client.get<PlatformMfaStatusResponse>(API_ROUTES.platformAuth.mfaStatus);
    return response.data;
  }

  /** Starts (or restarts) MFA setup for the currently-authenticated Platform Admin. */
  async setupMfa(): Promise<PlatformMfaSetupResponse> {
    const response = await this.client.post<PlatformMfaSetupResponse>(API_ROUTES.platformAuth.mfaSetup);
    return response.data;
  }

  /** Confirms a pending setup, turning MFA on. */
  async enableMfa(request: PlatformMfaEnableRequest): Promise<void> {
    await this.client.post<void>(API_ROUTES.platformAuth.mfaEnable, request);
  }

  /** Turns MFA back off, after proving the caller still controls the authenticator. */
  async disableMfa(request: PlatformMfaDisableRequest): Promise<void> {
    await this.client.post<void>(API_ROUTES.platformAuth.mfaDisable, request);
  }

  /** Self-service password change for the currently-authenticated Platform Admin — proves
   * the current password, then rotates it. */
  async changePassword(request: PlatformChangePasswordRequest): Promise<void> {
    await this.client.post<void>(API_ROUTES.platformAuth.changePassword, request);
  }

  /** Revokes the current token server-side — see JwtConfiguration/PlatformAuthController's
   * OnTokenValidated/Logout for the backend half of this. */
  async logout(): Promise<void> {
    await this.client.post<void>(API_ROUTES.platformAuth.logout);
  }
}
