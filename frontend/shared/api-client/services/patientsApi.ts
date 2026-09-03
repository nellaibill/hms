import { API_ROUTES } from '../../constants';
import type {
  AddAllergyRequest,
  CreatePatientRequest,
  CreatePatientVisitRequest,
  Patient,
  PatientListQuery,
  PatientVisit,
  UpdatePatientRequest,
} from '../../dtos';
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
        requiresDataVerification: query.requiresDataVerification,
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

  /** Adds one allergy row to an existing patient — mirrors HMS.Modules.Patients.Endpoints.PatientsController's POST .../allergies. */
  async addAllergy(id: string, request: AddAllergyRequest): Promise<Patient> {
    const response = await this.client.post<Patient>(API_ROUTES.patients.allergies(id), request);
    return response.data;
  }

  /** Removes one allergy row — mirrors the DELETE .../allergies/{allergyId} endpoint. */
  async removeAllergy(id: string, allergyId: string): Promise<Patient> {
    const response = await this.client.delete<Patient>(API_ROUTES.patients.allergyById(id, allergyId));
    return response.data;
  }

  /** Records one visit ("Registration Details") for an existing patient — mirrors
   * HMS.Modules.Patients.Endpoints.PatientVisitsController's POST .../visits. */
  async createVisit(id: string, request: CreatePatientVisitRequest): Promise<PatientVisit> {
    const response = await this.client.post<PatientVisit>(API_ROUTES.patients.visits(id), request);
    return response.data;
  }

  /** Lists every visit recorded for a patient, newest first — mirrors PatientVisitsController's
   * GET .../visits (list). */
  async getVisits(id: string): Promise<PatientVisit[]> {
    const response = await this.client.get<PatientVisit[]>(API_ROUTES.patients.visits(id));
    return response.data;
  }
}
