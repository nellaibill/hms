import { API_ROUTES } from '../../constants';
import type {
  BillableInvestigation,
  CreateOpdDiagnosisRequest,
  OpdConsultationDetail,
  OpdConsultationNote,
  OpdDiagnosisOption,
  OpdInvestigationDepartment,
  OpdInvestigationServiceOption,
  SaveOpdConsultationRequest,
  StructureConsultationNoteRequest,
  StructuredConsultationNoteFields,
} from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the OpdConsultation module — the clinical note (vitals, diagnosis,
 * investigations, plan) behind the OPD Patient List's "Consult" action. Built on the shared
 * HTTP client. Feature code (web/mobile) calls this, never the HTTP client directly —
 * docs/FrontendArchitecture.md §6. Mirrors
 * HMS.Modules.OpdConsultation.Endpoints.OpdConsultationsController's exact route list.
 */
export class OpdConsultationApi {
  constructor(private readonly client: HttpClient) {}

  /** Fetches (auto-creating on the backend's first call) the note plus its read-only header. */
  async getOrCreate(consultationId: string): Promise<OpdConsultationDetail> {
    const response = await this.client.get<OpdConsultationDetail>(API_ROUTES.opdConsultations.byConsultationId(consultationId));
    return response.data;
  }

  /** Every consultation note already recorded for a patient (newest first), each with its
   * header — read-only, unlike getOrCreate it never creates a note. Backs Patient Details'
   * Medical Information tab. */
  /** Active Diagnosis catalog entries (by name or ICD code) for the consultation form's picker. */
  async searchDiagnoses(search?: string): Promise<OpdDiagnosisOption[]> {
    const response = await this.client.get<OpdDiagnosisOption[]>(API_ROUTES.opdConsultations.diagnoses, { query: search ? { search } : undefined });
    return response.data;
  }

  /** Adds a diagnosis to the catalog from the consultation form (returns the existing entry if the
   * name is already there). */
  async createDiagnosis(request: CreateOpdDiagnosisRequest): Promise<OpdDiagnosisOption> {
    const response = await this.client.post<OpdDiagnosisOption>(API_ROUTES.opdConsultations.diagnoses, request);
    return response.data;
  }

  /** Every active Laboratory or Radiology catalog service, for the investigation picker. */
  async listInvestigationServices(department: OpdInvestigationDepartment): Promise<OpdInvestigationServiceOption[]> {
    const response = await this.client.get<OpdInvestigationServiceOption[]>(API_ROUTES.opdConsultations.investigationServices, { query: { department } });
    return response.data;
  }

  /** Catalog-linked investigations ordered on one visit — OPD Billing Entry pre-adds these. */
  async listBillableInvestigations(visitId: string): Promise<BillableInvestigation[]> {
    const response = await this.client.get<BillableInvestigation[]>(API_ROUTES.opdConsultations.billableInvestigations, { query: { visitId } });
    return response.data;
  }

  async listByPatient(patientId: string): Promise<OpdConsultationDetail[]> {
    const response = await this.client.get<OpdConsultationDetail[]>(API_ROUTES.opdConsultations.base, { query: { patientId } });
    return response.data;
  }

  async saveDraft(consultationId: string, request: SaveOpdConsultationRequest): Promise<OpdConsultationNote> {
    const response = await this.client.post<OpdConsultationNote>(API_ROUTES.opdConsultations.saveDraft(consultationId), request);
    return response.data;
  }

  async complete(consultationId: string, request: SaveOpdConsultationRequest): Promise<OpdConsultationNote> {
    const response = await this.client.post<OpdConsultationNote>(API_ROUTES.opdConsultations.complete(consultationId), request);
    return response.data;
  }

  /** Moves a Completed note back to Draft so it can be edited again. */
  async reopen(consultationId: string): Promise<OpdConsultationNote> {
    const response = await this.client.post<OpdConsultationNote>(API_ROUTES.opdConsultations.reopen(consultationId), {});
    return response.data;
  }

  /** Structures a dictated/typed transcript into the note's narrative fields — the result isn't
   * persisted by this call; the caller merges it into the form and still submits through
   * saveDraft/complete as normal. */
  async structureNote(consultationId: string, request: StructureConsultationNoteRequest): Promise<StructuredConsultationNoteFields> {
    const response = await this.client.post<StructuredConsultationNoteFields>(API_ROUTES.opdConsultations.structureNote(consultationId), request);
    return response.data;
  }
}
