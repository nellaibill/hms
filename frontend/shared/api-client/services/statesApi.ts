import { API_ROUTES } from '../../constants';
import type { District, State } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for India's state/district reference data, built on the shared HTTP
 * client. Read-only — no admin CRUD in this iteration, only the seeded list (see
 * HMS.Modules.Masters.Infrastructure.Configurations.StateConfiguration/DistrictConfiguration).
 */
export class StatesApi {
  constructor(private readonly client: HttpClient) {}

  async getStates(): Promise<State[]> {
    const response = await this.client.get<State[]>(API_ROUTES.states.base);
    return response.data;
  }

  async getDistricts(stateId: string): Promise<District[]> {
    const response = await this.client.get<District[]>(API_ROUTES.states.districts(stateId));
    return response.data;
  }
}
