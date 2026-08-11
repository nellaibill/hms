import { API_ROUTES } from '../../constants';
import type { AppointmentType, AppointmentTypeListQuery, CreateAppointmentTypeRequest, UpdateAppointmentTypeRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedAppointmentTypes {
  items: AppointmentType[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Appointment Type directory, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class AppointmentTypesApi {
  constructor(private readonly client: HttpClient) {}

  async getAppointmentTypes(query: AppointmentTypeListQuery = {}): Promise<PagedAppointmentTypes> {
    const response = await this.client.get<AppointmentType[]>(API_ROUTES.masters.appointmentType.base, {
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

  async getAppointmentTypeById(id: string): Promise<AppointmentType> {
    const response = await this.client.get<AppointmentType>(API_ROUTES.masters.appointmentType.byId(id));
    return response.data;
  }

  async createAppointmentType(request: CreateAppointmentTypeRequest): Promise<AppointmentType> {
    const response = await this.client.post<AppointmentType>(API_ROUTES.masters.appointmentType.base, request);
    return response.data;
  }

  async updateAppointmentType(id: string, request: UpdateAppointmentTypeRequest): Promise<AppointmentType> {
    const response = await this.client.put<AppointmentType>(API_ROUTES.masters.appointmentType.byId(id), request);
    return response.data;
  }
}
