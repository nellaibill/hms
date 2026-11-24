import { API_ROUTES } from '../../constants';
import type { ChangePasswordRequest, CurrentFeaturesResponse, LoginRequest, LoginResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Authentication module, built on the shared HTTP client. Feature
 * code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class AuthApi {
  constructor(private readonly client: HttpClient) {}

  async login(hospitalCode: string, request: LoginRequest): Promise<LoginResponse> {
    // skipUnauthorizedHandling — a 401 here means "wrong credentials", not "your existing
    // session expired" (there is no session yet), so it must not trigger the client's global
    // logout-and-redirect handling.
    const response = await this.client.post<LoginResponse>(API_ROUTES.auth.login, request, {
      headers: { 'X-Hospital-Code': hospitalCode },
      skipUnauthorizedHandling: true,
    });
    return response.data;
  }

  /** The hospital's feature keys right now (ADR-145), read live rather than from the login. */
  async getFeatures(): Promise<string[]> {
    const response = await this.client.get<CurrentFeaturesResponse>(API_ROUTES.auth.features);
    return response.data.featureKeys;
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await this.client.post<void>(API_ROUTES.auth.changePassword, request);
  }
}
