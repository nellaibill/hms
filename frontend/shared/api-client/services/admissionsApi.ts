import { API_ROUTES } from '../../constants';
import type {
  Admission,
  AdmissionBedStay,
  AdmissionCharge,
  AdmissionListQuery,
  BedTransferHistory,
  CreateAdmissionChargeRequest,
  CreateAdmissionRequest,
  DischargeAdmissionRequest,
  TransferBedRequest,
  UpdateAdmissionRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedAdmissions {
  items: Admission[];
  meta: PaginationMeta;
}

/**
 * Typed API service for IPD Admissions (admit/transfer-bed/discharge workflow) and their
 * charge line items, built on the shared HTTP client. Feature code (web/mobile) calls this,
 * never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class AdmissionsApi {
  constructor(private readonly client: HttpClient) {}

  async getAdmissions(query: AdmissionListQuery = {}): Promise<PagedAdmissions> {
    const response = await this.client.get<Admission[]>(API_ROUTES.ipd.admissions.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        status: query.status,
        wardId: query.wardId,
        departmentId: query.departmentId,
        consultantId: query.consultantId,
      },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getAdmissionById(id: string): Promise<Admission> {
    const response = await this.client.get<Admission>(API_ROUTES.ipd.admissions.byId(id));
    return response.data;
  }

  async createAdmission(request: CreateAdmissionRequest): Promise<Admission> {
    const response = await this.client.post<Admission>(API_ROUTES.ipd.admissions.base, request);
    return response.data;
  }

  async updateAdmission(id: string, request: UpdateAdmissionRequest): Promise<Admission> {
    const response = await this.client.put<Admission>(API_ROUTES.ipd.admissions.byId(id), request);
    return response.data;
  }

  async transferBed(id: string, request: TransferBedRequest): Promise<Admission> {
    const response = await this.client.post<Admission>(API_ROUTES.ipd.admissions.transferBed(id), request);
    return response.data;
  }

  async discharge(id: string, request: DischargeAdmissionRequest): Promise<Admission> {
    const response = await this.client.post<Admission>(API_ROUTES.ipd.admissions.discharge(id), request);
    return response.data;
  }

  async getTransferHistory(id: string): Promise<BedTransferHistory[]> {
    const response = await this.client.get<BedTransferHistory[]>(API_ROUTES.ipd.admissions.transferHistory(id));
    return response.data;
  }

  async getBedStayHistory(id: string): Promise<AdmissionBedStay[]> {
    const response = await this.client.get<AdmissionBedStay[]>(API_ROUTES.ipd.admissions.bedHistory(id));
    return response.data;
  }

  async getCharges(admissionId: string): Promise<AdmissionCharge[]> {
    const response = await this.client.get<AdmissionCharge[]>(API_ROUTES.ipd.admissions.charges(admissionId));
    return response.data;
  }

  async postCharge(admissionId: string, request: CreateAdmissionChargeRequest): Promise<AdmissionCharge> {
    const response = await this.client.post<AdmissionCharge>(API_ROUTES.ipd.admissions.charges(admissionId), request);
    return response.data;
  }
}
