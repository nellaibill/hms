import { API_ROUTES } from '../../constants';
import type { CreateDiagnosticProviderRequest, DiagnosticProvider, DiagnosticProviderListQuery, UpdateDiagnosticProviderRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDiagnosticProviders {
  items: DiagnosticProvider[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the DiagnosticProvider master (Central Laboratory's "External Lab"),
 * built on the shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client
 * directly — docs/FrontendArchitecture.md §6.
 */
export class DiagnosticProvidersApi {
  constructor(private readonly client: HttpClient) {}

  async getDiagnosticProviders(query: DiagnosticProviderListQuery = {}): Promise<PagedDiagnosticProviders> {
    const response = await this.client.get<DiagnosticProvider[]>(API_ROUTES.diagnostics.providers.base, {
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

  async getDiagnosticProviderById(id: string): Promise<DiagnosticProvider> {
    const response = await this.client.get<DiagnosticProvider>(API_ROUTES.diagnostics.providers.byId(id));
    return response.data;
  }

  async createDiagnosticProvider(request: CreateDiagnosticProviderRequest): Promise<DiagnosticProvider> {
    const response = await this.client.post<DiagnosticProvider>(API_ROUTES.diagnostics.providers.base, request);
    return response.data;
  }

  async updateDiagnosticProvider(id: string, request: UpdateDiagnosticProviderRequest): Promise<DiagnosticProvider> {
    const response = await this.client.put<DiagnosticProvider>(API_ROUTES.diagnostics.providers.byId(id), request);
    return response.data;
  }

  async deleteDiagnosticProvider(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.diagnostics.providers.byId(id));
  }
}
