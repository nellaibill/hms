import { API_ROUTES } from '../../constants';
import type { ConsultationType, ConsultationTypeListQuery, CreateConsultationTypeRequest, UpdateConsultationTypeRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedConsultationTypes {
  items: ConsultationType[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Consultation Type directory, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class ConsultationTypesApi {
  constructor(private readonly client: HttpClient) {}

  async getConsultationTypes(query: ConsultationTypeListQuery = {}): Promise<PagedConsultationTypes> {
    const response = await this.client.get<ConsultationType[]>(API_ROUTES.masters.consultationType.base, {
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

  async getConsultationTypeById(id: string): Promise<ConsultationType> {
    const response = await this.client.get<ConsultationType>(API_ROUTES.masters.consultationType.byId(id));
    return response.data;
  }

  async createConsultationType(request: CreateConsultationTypeRequest): Promise<ConsultationType> {
    const response = await this.client.post<ConsultationType>(API_ROUTES.masters.consultationType.base, request);
    return response.data;
  }

  async updateConsultationType(id: string, request: UpdateConsultationTypeRequest): Promise<ConsultationType> {
    const response = await this.client.put<ConsultationType>(API_ROUTES.masters.consultationType.byId(id), request);
    return response.data;
  }
}
