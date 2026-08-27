import { API_ROUTES } from '../../constants';
import type {
  AttendanceListQuery,
  AttendanceResponse,
  CheckInRequest,
  CheckOutRequest,
  CreateAttendanceRequest,
  UpdateAttendanceRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedAttendance {
  items: AttendanceResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for Attendance tracking (Hospital HR Management MVP), built on the
 * shared HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6.
 */
export class AttendanceApi {
  constructor(private readonly client: HttpClient) {}

  async getAttendance(query: AttendanceListQuery = {}): Promise<PagedAttendance> {
    const response = await this.client.get<AttendanceResponse[]>(API_ROUTES.attendance.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        employeeId: query.employeeId,
        departmentId: query.departmentId,
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

  async getAttendanceById(id: string): Promise<AttendanceResponse> {
    const response = await this.client.get<AttendanceResponse>(API_ROUTES.attendance.byId(id));
    return response.data;
  }

  async createAttendance(request: CreateAttendanceRequest): Promise<AttendanceResponse> {
    const response = await this.client.post<AttendanceResponse>(API_ROUTES.attendance.base, request);
    return response.data;
  }

  async updateAttendance(id: string, request: UpdateAttendanceRequest): Promise<AttendanceResponse> {
    const response = await this.client.put<AttendanceResponse>(API_ROUTES.attendance.byId(id), request);
    return response.data;
  }

  async checkIn(request: CheckInRequest): Promise<AttendanceResponse> {
    const response = await this.client.post<AttendanceResponse>(API_ROUTES.attendance.checkIn, request);
    return response.data;
  }

  async checkOut(request: CheckOutRequest): Promise<AttendanceResponse> {
    const response = await this.client.post<AttendanceResponse>(API_ROUTES.attendance.checkOut, request);
    return response.data;
  }
}
