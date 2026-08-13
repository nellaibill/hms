import { API_ROUTES } from '../../constants';
import type { CreateWardRequest, UpdateWardRequest, Ward, WardListQuery } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedWards {
  items: Ward[];
  meta: PaginationMeta;
}

/**
 * Typed API service for IPD's Ward Master, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class WardsApi {
  constructor(private readonly client: HttpClient) {}

  async getWards(query: WardListQuery = {}): Promise<PagedWards> {
    const response = await this.client.get<Ward[]>(API_ROUTES.ipd.wards.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
        departmentId: query.departmentId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getWardById(id: string): Promise<Ward> {
    const response = await this.client.get<Ward>(API_ROUTES.ipd.wards.byId(id));
    return response.data;
  }

  async createWard(request: CreateWardRequest): Promise<Ward> {
    const response = await this.client.post<Ward>(API_ROUTES.ipd.wards.base, request);
    return response.data;
  }

  async updateWard(id: string, request: UpdateWardRequest): Promise<Ward> {
    const response = await this.client.put<Ward>(API_ROUTES.ipd.wards.byId(id), request);
    return response.data;
  }

  async deleteWard(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.ipd.wards.byId(id));
  }
}
