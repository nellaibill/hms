import { API_ROUTES } from '../../constants';
import type { CreateDiagnosticCategoryRequest, DiagnosticCategory, DiagnosticCategoryListQuery, UpdateDiagnosticCategoryRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDiagnosticCategories {
  items: DiagnosticCategory[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the DiagnosticCategory master (Central Laboratory), built on the
 * shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6.
 */
export class DiagnosticCategoriesApi {
  constructor(private readonly client: HttpClient) {}

  async getDiagnosticCategories(query: DiagnosticCategoryListQuery = {}): Promise<PagedDiagnosticCategories> {
    const response = await this.client.get<DiagnosticCategory[]>(API_ROUTES.diagnostics.categories.base, {
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

  async getDiagnosticCategoryById(id: string): Promise<DiagnosticCategory> {
    const response = await this.client.get<DiagnosticCategory>(API_ROUTES.diagnostics.categories.byId(id));
    return response.data;
  }

  async createDiagnosticCategory(request: CreateDiagnosticCategoryRequest): Promise<DiagnosticCategory> {
    const response = await this.client.post<DiagnosticCategory>(API_ROUTES.diagnostics.categories.base, request);
    return response.data;
  }

  async updateDiagnosticCategory(id: string, request: UpdateDiagnosticCategoryRequest): Promise<DiagnosticCategory> {
    const response = await this.client.put<DiagnosticCategory>(API_ROUTES.diagnostics.categories.byId(id), request);
    return response.data;
  }

  async deleteDiagnosticCategory(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.diagnostics.categories.byId(id));
  }
}
