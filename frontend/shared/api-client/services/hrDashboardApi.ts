import { API_ROUTES } from '../../constants';
import type { HrDashboardResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the HR dashboard snapshot (Hospital HR Management MVP), built on the
 * shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6.
 */
export class HrDashboardApi {
  constructor(private readonly client: HttpClient) {}

  async getDashboard(): Promise<HrDashboardResponse> {
    const response = await this.client.get<HrDashboardResponse>(API_ROUTES.hrDashboard.base);
    return response.data;
  }
}
