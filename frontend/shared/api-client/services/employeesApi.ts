import { API_ROUTES } from '../../constants';
import type {
  CreateEmployeeRequest,
  EmployeeLeaveBalanceResponse,
  EmployeeListQuery,
  EmployeeResponse,
  UpdateEmployeeRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedEmployees {
  items: EmployeeResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Employee master (Hospital HR Management MVP), built on the
 * shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6.
 */
export class EmployeesApi {
  constructor(private readonly client: HttpClient) {}

  async getEmployees(query: EmployeeListQuery = {}): Promise<PagedEmployees> {
    const response = await this.client.get<EmployeeResponse[]>(API_ROUTES.employees.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        departmentId: query.departmentId,
        designationId: query.designationId,
        employeeType: query.employeeType,
        employmentStatus: query.employmentStatus,
        isActive: query.isActive,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getEmployeeById(id: string): Promise<EmployeeResponse> {
    const response = await this.client.get<EmployeeResponse>(API_ROUTES.employees.byId(id));
    return response.data;
  }

  async createEmployee(request: CreateEmployeeRequest): Promise<EmployeeResponse> {
    const response = await this.client.post<EmployeeResponse>(API_ROUTES.employees.base, request);
    return response.data;
  }

  async updateEmployee(id: string, request: UpdateEmployeeRequest): Promise<EmployeeResponse> {
    const response = await this.client.put<EmployeeResponse>(API_ROUTES.employees.byId(id), request);
    return response.data;
  }

  async deleteEmployee(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.employees.byId(id));
  }

  async activateEmployee(id: string): Promise<EmployeeResponse> {
    const response = await this.client.post<EmployeeResponse>(API_ROUTES.employees.activate(id));
    return response.data;
  }

  async deactivateEmployee(id: string): Promise<EmployeeResponse> {
    const response = await this.client.post<EmployeeResponse>(API_ROUTES.employees.deactivate(id));
    return response.data;
  }

  async getEmployeeLeaveBalances(id: string): Promise<EmployeeLeaveBalanceResponse[]> {
    const response = await this.client.get<EmployeeLeaveBalanceResponse[]>(API_ROUTES.employees.leaveBalances(id));
    return response.data;
  }
}
