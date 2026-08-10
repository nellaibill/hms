import { API_ROUTES } from '../../constants';
import type { ConsultantListQuery, Consultant, CreateConsultantRequest, UpdateConsultantRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedConsultants {
  items: Consultant[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Consultant directory, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class ConsultantsApi {
  constructor(private readonly client: HttpClient) {}

  async getConsultants(query: ConsultantListQuery = {}): Promise<PagedConsultants> {
    const response = await this.client.get<Consultant[]>(API_ROUTES.masters.consultant.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
        departmentId: query.departmentId,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getConsultantById(id: string): Promise<Consultant> {
    const response = await this.client.get<Consultant>(API_ROUTES.masters.consultant.byId(id));
    return response.data;
  }

  async createConsultant(request: CreateConsultantRequest): Promise<Consultant> {
    const response = await this.client.post<Consultant>(API_ROUTES.masters.consultant.base, request);
    return response.data;
  }

  async updateConsultant(id: string, request: UpdateConsultantRequest): Promise<Consultant> {
    const response = await this.client.put<Consultant>(API_ROUTES.masters.consultant.byId(id), request);
    return response.data;
  }
}
