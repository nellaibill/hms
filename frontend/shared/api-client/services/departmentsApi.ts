import { API_ROUTES } from '../../constants';
import type { CreateDepartmentRequest, Department, DepartmentListQuery, UpdateDepartmentRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDepartments {
  items: Department[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Department directory, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class DepartmentsApi {
  constructor(private readonly client: HttpClient) {}

  async getDepartments(query: DepartmentListQuery = {}): Promise<PagedDepartments> {
    const response = await this.client.get<Department[]>(API_ROUTES.departments.base, {
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

  async getDepartmentById(id: string): Promise<Department> {
    const response = await this.client.get<Department>(API_ROUTES.departments.byId(id));
    return response.data;
  }

  async createDepartment(request: CreateDepartmentRequest): Promise<Department> {
    const response = await this.client.post<Department>(API_ROUTES.departments.base, request);
    return response.data;
  }

  async updateDepartment(id: string, request: UpdateDepartmentRequest): Promise<Department> {
    const response = await this.client.put<Department>(API_ROUTES.departments.byId(id), request);
    return response.data;
  }

  async deleteDepartment(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.departments.byId(id));
  }
}
