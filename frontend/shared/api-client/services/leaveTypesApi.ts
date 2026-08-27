import { API_ROUTES } from '../../constants';
import type { CreateLeaveTypeRequest, LeaveTypeListQuery, LeaveTypeResponse, UpdateLeaveTypeRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedLeaveTypes {
  items: LeaveTypeResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the LeaveType master (Hospital HR Management MVP), built on the
 * shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6.
 */
export class LeaveTypesApi {
  constructor(private readonly client: HttpClient) {}

  async getLeaveTypes(query: LeaveTypeListQuery = {}): Promise<PagedLeaveTypes> {
    const response = await this.client.get<LeaveTypeResponse[]>(API_ROUTES.leaveTypes.base, {
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

  async getLeaveTypeById(id: string): Promise<LeaveTypeResponse> {
    const response = await this.client.get<LeaveTypeResponse>(API_ROUTES.leaveTypes.byId(id));
    return response.data;
  }

  async createLeaveType(request: CreateLeaveTypeRequest): Promise<LeaveTypeResponse> {
    const response = await this.client.post<LeaveTypeResponse>(API_ROUTES.leaveTypes.base, request);
    return response.data;
  }

  async updateLeaveType(id: string, request: UpdateLeaveTypeRequest): Promise<LeaveTypeResponse> {
    const response = await this.client.put<LeaveTypeResponse>(API_ROUTES.leaveTypes.byId(id), request);
    return response.data;
  }

  async deleteLeaveType(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.leaveTypes.byId(id));
  }
}
