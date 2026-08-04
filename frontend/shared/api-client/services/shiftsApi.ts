import { API_ROUTES } from '../../constants';
import type { CreateShiftRequest, Shift, ShiftListQuery, UpdateShiftRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedShifts {
  items: Shift[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Shift Management module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class ShiftsApi {
  constructor(private readonly client: HttpClient) {}

  async getShifts(query: ShiftListQuery = {}): Promise<PagedShifts> {
    const response = await this.client.get<Shift[]>(API_ROUTES.shifts.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getShiftById(id: string): Promise<Shift> {
    const response = await this.client.get<Shift>(API_ROUTES.shifts.byId(id));
    return response.data;
  }

  async createShift(request: CreateShiftRequest): Promise<Shift> {
    const response = await this.client.post<Shift>(API_ROUTES.shifts.base, request);
    return response.data;
  }

  async updateShift(id: string, request: UpdateShiftRequest): Promise<Shift> {
    const response = await this.client.put<Shift>(API_ROUTES.shifts.byId(id), request);
    return response.data;
  }

  async deleteShift(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.shifts.byId(id));
  }
}
