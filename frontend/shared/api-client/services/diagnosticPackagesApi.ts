import { API_ROUTES } from '../../constants';
import type {
  AddDiagnosticPackageItemRequest,
  CreateDiagnosticPackageRequest,
  DiagnosticPackage,
  DiagnosticPackageListQuery,
  UpdateDiagnosticPackageRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDiagnosticPackages {
  items: DiagnosticPackage[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the DiagnosticPackage master (Central Laboratory's test bundles),
 * built on the shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client
 * directly — docs/FrontendArchitecture.md §6.
 */
export class DiagnosticPackagesApi {
  constructor(private readonly client: HttpClient) {}

  async getDiagnosticPackages(query: DiagnosticPackageListQuery = {}): Promise<PagedDiagnosticPackages> {
    const response = await this.client.get<DiagnosticPackage[]>(API_ROUTES.diagnostics.packages.base, {
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

  async getDiagnosticPackageById(id: string): Promise<DiagnosticPackage> {
    const response = await this.client.get<DiagnosticPackage>(API_ROUTES.diagnostics.packages.byId(id));
    return response.data;
  }

  async createDiagnosticPackage(request: CreateDiagnosticPackageRequest): Promise<DiagnosticPackage> {
    const response = await this.client.post<DiagnosticPackage>(API_ROUTES.diagnostics.packages.base, request);
    return response.data;
  }

  async updateDiagnosticPackage(id: string, request: UpdateDiagnosticPackageRequest): Promise<DiagnosticPackage> {
    const response = await this.client.put<DiagnosticPackage>(API_ROUTES.diagnostics.packages.byId(id), request);
    return response.data;
  }

  async deleteDiagnosticPackage(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.diagnostics.packages.byId(id));
  }

  async addDiagnosticPackageItem(packageId: string, request: AddDiagnosticPackageItemRequest): Promise<DiagnosticPackage> {
    const response = await this.client.post<DiagnosticPackage>(API_ROUTES.diagnostics.packages.items(packageId), request);
    return response.data;
  }

  async removeDiagnosticPackageItem(packageId: string, itemId: string): Promise<DiagnosticPackage> {
    const response = await this.client.delete<DiagnosticPackage>(API_ROUTES.diagnostics.packages.itemById(packageId, itemId));
    return response.data;
  }
}
