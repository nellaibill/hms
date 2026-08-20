import { API_ROUTES } from '../../constants';
import type { CreatePatientRequest, Patient, PatientListQuery, PatientRegistration, UpdatePatientRequest } from '../../dtos';
import type { IdProofType } from '../../enums';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedPatients {
  items: Patient[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Patients module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class PatientsApi {
  constructor(private readonly client: HttpClient) {}

  async getPatients(query: PatientListQuery = {}): Promise<PagedPatients> {
    const response = await this.client.get<Patient[]>(API_ROUTES.patients.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        name: query.name,
        age: query.age,
        uhid: query.uhid,
        phone: query.phone,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getPatientById(id: string): Promise<Patient> {
    const response = await this.client.get<Patient>(API_ROUTES.patients.byId(id));
    return response.data;
  }

  async createPatient(request: CreatePatientRequest): Promise<Patient> {
    const response = await this.client.post<Patient>(API_ROUTES.patients.base, request);
    return response.data;
  }

  async updatePatient(id: string, request: UpdatePatientRequest): Promise<Patient> {
    const response = await this.client.put<Patient>(API_ROUTES.patients.byId(id), request);
    return response.data;
  }

  async deletePatient(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.patients.byId(id));
  }

  async uploadPhoto(id: string, file: File): Promise<Patient> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await this.client.postFormData<Patient>(API_ROUTES.patients.photo(id), formData);
    return response.data;
  }

  async uploadIdProof(id: string, idProofType: IdProofType, file: File): Promise<Patient> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('idProofType', idProofType);
    const response = await this.client.postFormData<Patient>(API_ROUTES.patients.idProof(id), formData);
    return response.data;
  }

  /** All registrations/visits recorded for this patient over time, newest first — mirrors HMS.Modules.Patients.Endpoints.PatientsController's GET .../registrations. */
  async getRegistrations(id: string): Promise<PatientRegistration[]> {
    const response = await this.client.get<PatientRegistration[]>(API_ROUTES.patients.registrations(id));
    return response.data;
  }
}
