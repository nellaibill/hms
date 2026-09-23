import { API_ROUTES } from '../../constants';
import type {
  OpdConsultationDetail,
  OpdConsultationNote,
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
