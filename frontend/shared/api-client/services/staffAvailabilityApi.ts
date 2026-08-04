import { API_ROUTES } from '../../constants';
import type { CreateStaffAvailabilityRequest, StaffAvailability, StaffAvailabilityListQuery, UpdateStaffAvailabilityRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedStaffAvailability {
  items: StaffAvailability[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Staff Availability module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class StaffAvailabilityApi {
  constructor(private readonly client: HttpClient) {}

  async getStaffAvailability(query: StaffAvailabilityListQuery = {}): Promise<PagedStaffAvailability> {
    const response = await this.client.get<StaffAvailability[]>(API_ROUTES.staffAvailability.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getStaffAvailabilityById(id: string): Promise<StaffAvailability> {
    const response = await this.client.get<StaffAvailability>(API_ROUTES.staffAvailability.byId(id));
    return response.data;
  }

  async createStaffAvailability(request: CreateStaffAvailabilityRequest): Promise<StaffAvailability> {
    const response = await this.client.post<StaffAvailability>(API_ROUTES.staffAvailability.base, request);
    return response.data;
  }

  async updateStaffAvailability(id: string, request: UpdateStaffAvailabilityRequest): Promise<StaffAvailability> {
    const response = await this.client.put<StaffAvailability>(API_ROUTES.staffAvailability.byId(id), request);
    return response.data;
  }

  async deleteStaffAvailability(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.staffAvailability.byId(id));
  }
}
