import { API_ROUTES } from '../../constants';
import type { CreateShiftAssignmentRequest, ShiftAssignment, ShiftAssignmentListQuery, UpdateShiftAssignmentRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedShiftAssignments {
  items: ShiftAssignment[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Shift Assignment module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class ShiftAssignmentsApi {
  constructor(private readonly client: HttpClient) {}

  async getShiftAssignments(query: ShiftAssignmentListQuery = {}): Promise<PagedShiftAssignments> {
    const response = await this.client.get<ShiftAssignment[]>(API_ROUTES.shiftAssignments.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        departmentId: query.departmentId,
        rosterDateFrom: query.rosterDateFrom,
        rosterDateTo: query.rosterDateTo,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getShiftAssignmentById(id: string): Promise<ShiftAssignment> {
    const response = await this.client.get<ShiftAssignment>(API_ROUTES.shiftAssignments.byId(id));
    return response.data;
  }

  async createShiftAssignment(request: CreateShiftAssignmentRequest): Promise<ShiftAssignment> {
    const response = await this.client.post<ShiftAssignment>(API_ROUTES.shiftAssignments.base, request);
    return response.data;
  }

  async updateShiftAssignment(id: string, request: UpdateShiftAssignmentRequest): Promise<ShiftAssignment> {
    const response = await this.client.put<ShiftAssignment>(API_ROUTES.shiftAssignments.byId(id), request);
    return response.data;
  }

  async deleteShiftAssignment(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.shiftAssignments.byId(id));
  }
}
