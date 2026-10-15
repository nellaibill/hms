import { API_ROUTES } from '../../constants';
import type {
  Appointment,
  AppointmentListQuery,
  AppointmentSlots,
  AppointmentSummary,
  BookAppointmentRequest,
  CancelAppointmentRequest,
  ConsultantBlock,
  ConsultantBlockCreated,
  ConsultantBlockListQuery,
  CreateConsultantBlockRequest,
  MarkAppointmentArrivedRequest,
  RescheduleAppointmentRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedAppointments {
  items: Appointment[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Appointments module. Mirrors
 * HMS.Modules.Appointments.Endpoints.AppointmentsController.
 */
export class AppointmentsApi {
  constructor(private readonly client: HttpClient) {}

  async getAppointments(query: AppointmentListQuery = {}): Promise<PagedAppointments> {
    const response = await this.client.get<Appointment[]>(API_ROUTES.appointments.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        fromDate: query.fromDate,
        toDate: query.toDate,
        consultantId: query.consultantId,
        departmentId: query.departmentId,
        patientId: query.patientId,
        status: query.status,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getAppointmentById(id: string): Promise<Appointment> {
    const response = await this.client.get<Appointment>(API_ROUTES.appointments.byId(id));
    return response.data;
  }

  /** One consultant's slots for one day ("yyyy-MM-dd"), with seats booked in each. */
  async getSlots(consultantId: string, date: string): Promise<AppointmentSlots> {
    const response = await this.client.get<AppointmentSlots>(API_ROUTES.appointments.slots, { query: { consultantId, date } });
    return response.data;
  }

  async getSummary(date: string, filters: { consultantId?: string; departmentId?: string } = {}): Promise<AppointmentSummary> {
    const response = await this.client.get<AppointmentSummary>(API_ROUTES.appointments.summary, { query: { date, ...filters } });
    return response.data;
  }

  async book(request: BookAppointmentRequest): Promise<Appointment> {
    const response = await this.client.post<Appointment>(API_ROUTES.appointments.base, request);
    return response.data;
  }

  async reschedule(id: string, request: RescheduleAppointmentRequest): Promise<Appointment> {
    const response = await this.client.post<Appointment>(API_ROUTES.appointments.reschedule(id), request);
    return response.data;
  }

  async cancel(id: string, request: CancelAppointmentRequest): Promise<Appointment> {
    const response = await this.client.post<Appointment>(API_ROUTES.appointments.cancel(id), request);
    return response.data;
  }

  async markNoShow(id: string): Promise<Appointment> {
    const response = await this.client.post<Appointment>(API_ROUTES.appointments.noShow(id));
    return response.data;
  }

  /** Check-in: links the visit reception just recorded for the patient to this appointment. */
  async markArrived(id: string, request: MarkAppointmentArrivedRequest): Promise<Appointment> {
    const response = await this.client.post<Appointment>(API_ROUTES.appointments.arrive(id), request);
    return response.data;
  }

  async getBlocks(query: ConsultantBlockListQuery = {}): Promise<ConsultantBlock[]> {
    const response = await this.client.get<ConsultantBlock[]>(API_ROUTES.appointments.blocks, {
      query: { consultantId: query.consultantId, fromDate: query.fromDate, toDate: query.toDate },
    });
    return response.data;
  }

  async createBlock(request: CreateConsultantBlockRequest): Promise<ConsultantBlockCreated> {
    const response = await this.client.post<ConsultantBlockCreated>(API_ROUTES.appointments.blocks, request);
    return response.data;
  }

  async removeBlock(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.appointments.blockById(id));
  }
}
