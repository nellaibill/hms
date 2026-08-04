import { API_ROUTES } from '../../constants';
import type { CreateSwapRequest, SwapRequest, SwapRequestListQuery, UpdateSwapRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedSwapRequests {
  items: SwapRequest[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Shift Swap Request module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class ShiftSwapRequestsApi {
  constructor(private readonly client: HttpClient) {}

  async getSwapRequests(query: SwapRequestListQuery = {}): Promise<PagedSwapRequests> {
    const response = await this.client.get<SwapRequest[]>(API_ROUTES.shiftSwapRequests.base, {
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

  async getSwapRequestById(id: string): Promise<SwapRequest> {
    const response = await this.client.get<SwapRequest>(API_ROUTES.shiftSwapRequests.byId(id));
    return response.data;
  }

  async createSwapRequest(request: CreateSwapRequest): Promise<SwapRequest> {
    const response = await this.client.post<SwapRequest>(API_ROUTES.shiftSwapRequests.base, request);
    return response.data;
  }

  async updateSwapRequest(id: string, request: UpdateSwapRequest): Promise<SwapRequest> {
    const response = await this.client.put<SwapRequest>(API_ROUTES.shiftSwapRequests.byId(id), request);
    return response.data;
  }

  async deleteSwapRequest(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.shiftSwapRequests.byId(id));
  }
}
