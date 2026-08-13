import { API_ROUTES } from '../../constants';
import type { Bed, BedListQuery, CreateBedRequest, UpdateBedRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedBeds {
  items: Bed[];
  meta: PaginationMeta;
}

/**
 * Typed API service for IPD's Bed Master, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class BedsApi {
  constructor(private readonly client: HttpClient) {}

  async getBeds(query: BedListQuery = {}): Promise<PagedBeds> {
    const response = await this.client.get<Bed[]>(API_ROUTES.ipd.beds.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        wardId: query.wardId,
        status: query.status,
        isActive: query.isActive,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getBedById(id: string): Promise<Bed> {
    const response = await this.client.get<Bed>(API_ROUTES.ipd.beds.byId(id));
    return response.data;
  }

  /** Beds currently Available (and active), optionally scoped to one ward — backs the New
   * Admission and Bed Transfer bed pickers. */
  async getAvailableBeds(wardId?: string): Promise<Bed[]> {
    const response = await this.client.get<Bed[]>(API_ROUTES.ipd.beds.available, { query: { wardId } });
    return response.data;
  }

  async createBed(request: CreateBedRequest): Promise<Bed> {
    const response = await this.client.post<Bed>(API_ROUTES.ipd.beds.base, request);
    return response.data;
  }

  async updateBed(id: string, request: UpdateBedRequest): Promise<Bed> {
    const response = await this.client.put<Bed>(API_ROUTES.ipd.beds.byId(id), request);
    return response.data;
  }

  async deleteBed(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.ipd.beds.byId(id));
  }
}
