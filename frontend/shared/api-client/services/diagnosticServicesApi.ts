import { API_ROUTES } from '../../constants';
import type { CreateDiagnosticServiceRequest, DiagnosticService, DiagnosticServiceListQuery, UpdateDiagnosticServiceRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDiagnosticServices {
  items: DiagnosticService[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the DiagnosticService master (Central Laboratory/Radiology's typed
 * test catalog — replaces the old untyped DiagnosticTest for Laboratory/Radiology billing),
 * built on the shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client
 * directly — docs/FrontendArchitecture.md §6.
 */
export class DiagnosticServicesApi {
  constructor(private readonly client: HttpClient) {}

  async getDiagnosticServices(query: DiagnosticServiceListQuery = {}): Promise<PagedDiagnosticServices> {
    const response = await this.client.get<DiagnosticService[]>(API_ROUTES.diagnostics.services.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        categoryId: query.categoryId,
        serviceType: query.serviceType,
        isOutsourced: query.isOutsourced,
        isActive: query.isActive,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getDiagnosticServiceById(id: string): Promise<DiagnosticService> {
    const response = await this.client.get<DiagnosticService>(API_ROUTES.diagnostics.services.byId(id));
    return response.data;
  }

  async createDiagnosticService(request: CreateDiagnosticServiceRequest): Promise<DiagnosticService> {
    const response = await this.client.post<DiagnosticService>(API_ROUTES.diagnostics.services.base, request);
    return response.data;
  }

  async updateDiagnosticService(id: string, request: UpdateDiagnosticServiceRequest): Promise<DiagnosticService> {
    const response = await this.client.put<DiagnosticService>(API_ROUTES.diagnostics.services.byId(id), request);
    return response.data;
  }

  async deleteDiagnosticService(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.diagnostics.services.byId(id));
  }
}
