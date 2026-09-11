import { API_ROUTES } from '../../constants';
import type {
  OpdConsultationSummaryItem,
  OpdConsultationSummaryQuery,
  OpdPatientListItem,
  OpdPatientListQuery,
  VisitConsultationResponse,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedOpdPatients {
  items: OpdPatientListItem[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the OPD module (Patients module's outpatient consultation queue),
 * built on the shared HTTP client. Feature code (web/mobile) calls this, never the HTTP
 * client directly — docs/FrontendArchitecture.md §6. Mirrors
 * HMS.Modules.Patients.Endpoints.OpdController's exact route list.
 */
export class OpdApi {
  constructor(private readonly client: HttpClient) {}

  async getPatientList(query: OpdPatientListQuery = {}): Promise<PagedOpdPatients> {
    const response = await this.client.get<OpdPatientListItem[]>(API_ROUTES.opd.patients, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        from: query.from,
        to: query.to,
        departmentId: query.departmentId,
        consultantId: query.consultantId,
        status: query.status,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getConsultationSummary(query: OpdConsultationSummaryQuery = {}): Promise<OpdConsultationSummaryItem[]> {
    const response = await this.client.get<OpdConsultationSummaryItem[]>(API_ROUTES.opd.consultationsSummary, {
      query: {
        from: query.from,
        to: query.to,
        departmentId: query.departmentId,
      },
    });
    return response.data;
  }

  async checkIn(consultationId: string): Promise<VisitConsultationResponse> {
    const response = await this.client.post<VisitConsultationResponse>(API_ROUTES.opd.consultationCheckIn(consultationId), {});
    return response.data;
  }

  async startConsultation(consultationId: string): Promise<VisitConsultationResponse> {
    const response = await this.client.post<VisitConsultationResponse>(API_ROUTES.opd.consultationStart(consultationId), {});
    return response.data;
  }

  async complete(consultationId: string): Promise<VisitConsultationResponse> {
    const response = await this.client.post<VisitConsultationResponse>(API_ROUTES.opd.consultationComplete(consultationId), {});
    return response.data;
  }

  async cancel(consultationId: string): Promise<VisitConsultationResponse> {
    const response = await this.client.post<VisitConsultationResponse>(API_ROUTES.opd.consultationCancel(consultationId), {});
    return response.data;
  }

  async noShow(consultationId: string): Promise<VisitConsultationResponse> {
    const response = await this.client.post<VisitConsultationResponse>(API_ROUTES.opd.consultationNoShow(consultationId), {});
    return response.data;
  }
}
