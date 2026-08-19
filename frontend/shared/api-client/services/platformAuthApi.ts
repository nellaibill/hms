import { API_ROUTES } from '../../constants';
import type { PlatformLoginRequest, PlatformLoginResponse } from '../../dtos';
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

  /** Revokes the current token server-side — see JwtConfiguration/PlatformAuthController's
   * OnTokenValidated/Logout for the backend half of this. */
  async logout(): Promise<void> {
    await this.client.post<void>(API_ROUTES.platformAuth.logout);
  }
}
