import { API_ROUTES } from '../../constants';
import type { IpdDashboard } from '../../dtos';
import type { HttpClient } from '../httpClient';

/** Typed API service for the IPD dashboard's KPI tile — built on the shared HTTP client. */
export class IpdDashboardApi {
  constructor(private readonly client: HttpClient) {}

  async getDashboard(): Promise<IpdDashboard> {
    const response = await this.client.get<IpdDashboard>(API_ROUTES.ipd.dashboard);
    return response.data;
  }
}
