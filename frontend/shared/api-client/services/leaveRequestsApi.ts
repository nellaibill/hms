import { API_ROUTES } from '../../constants';
import type {
  ApproveLeaveRequestRequest,
  CreateLeaveRequestRequest,
  LeaveRequestListQuery,
  LeaveRequestResponse,
  RejectLeaveRequestRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedLeaveRequests {
  items: LeaveRequestResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the employee leave request workflow (Hospital HR Management MVP),
 * built on the shared HTTP client. Feature code (web/mobile) calls this, never the HTTP
 * client directly — docs/FrontendArchitecture.md §6.
 */
export class LeaveRequestsApi {
  constructor(private readonly client: HttpClient) {}

  async getLeaveRequests(query: LeaveRequestListQuery = {}): Promise<PagedLeaveRequests> {
    const response = await this.client.get<LeaveRequestResponse[]>(API_ROUTES.leaveRequests.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        employeeId: query.employeeId,
        leaveTypeId: query.leaveTypeId,
        status: query.status,
        dateFrom: query.dateFrom,
        dateTo: query.dateTo,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getLeaveRequestById(id: string): Promise<LeaveRequestResponse> {
    const response = await this.client.get<LeaveRequestResponse>(API_ROUTES.leaveRequests.byId(id));
    return response.data;
  }

  async createLeaveRequest(request: CreateLeaveRequestRequest): Promise<LeaveRequestResponse> {
    const response = await this.client.post<LeaveRequestResponse>(API_ROUTES.leaveRequests.base, request);
    return response.data;
  }

  async approveLeaveRequest(id: string, request: ApproveLeaveRequestRequest = {}): Promise<LeaveRequestResponse> {
    const response = await this.client.post<LeaveRequestResponse>(API_ROUTES.leaveRequests.approve(id), request);
    return response.data;
  }

  async rejectLeaveRequest(id: string, request: RejectLeaveRequestRequest): Promise<LeaveRequestResponse> {
    const response = await this.client.post<LeaveRequestResponse>(API_ROUTES.leaveRequests.reject(id), request);
    return response.data;
  }

  async cancelLeaveRequest(id: string): Promise<LeaveRequestResponse> {
    const response = await this.client.post<LeaveRequestResponse>(API_ROUTES.leaveRequests.cancel(id));
    return response.data;
  }
}
