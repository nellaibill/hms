import { API_ROUTES } from '../../constants';
import type { ChangePasswordRequest, LoginRequest, LoginResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Authentication module, built on the shared HTTP client. Feature
 * code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class AuthApi {
  constructor(private readonly client: HttpClient) {}

  async login(hospitalCode: string, request: LoginRequest): Promise<LoginResponse> {
    const response = await this.client.post<LoginResponse>(API_ROUTES.auth.login, request, {
      headers: { 'X-Hospital-Code': hospitalCode },
    });
    return response.data;
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await this.client.post<void>(API_ROUTES.auth.changePassword, request);
  }
}
