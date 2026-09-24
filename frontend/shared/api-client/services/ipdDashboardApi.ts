import { API_ROUTES } from '../../constants';
import type { IpdDashboard, MonthlyTotal } from '../../dtos';
import type { HttpClient } from '../httpClient';

/** Typed API service for the IPD dashboard's KPI tile — built on the shared HTTP client. */
export class IpdDashboardApi {
  constructor(private readonly client: HttpClient) {}

  async getDashboard(): Promise<IpdDashboard> {
    const response = await this.client.get<IpdDashboard>(API_ROUTES.ipd.dashboard);
    return response.data;
  }

  /** Admissions per month for the last `months` months, oldest first (Executive Dashboard). */
  async getMonthlyAdmissions(months = 6): Promise<MonthlyTotal[]> {
    const response = await this.client.get<MonthlyTotal[]>(API_ROUTES.ipd.monthlyAdmissions, { query: { months } });
    return response.data;
  }
}
