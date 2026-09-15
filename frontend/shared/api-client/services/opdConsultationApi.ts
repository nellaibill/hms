import { API_ROUTES } from '../../constants';
import type { OpdConsultationDetail, OpdConsultationNote, SaveOpdConsultationRequest } from '../../dtos';
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

  async saveDraft(consultationId: string, request: SaveOpdConsultationRequest): Promise<OpdConsultationNote> {
    const response = await this.client.post<OpdConsultationNote>(API_ROUTES.opdConsultations.saveDraft(consultationId), request);
    return response.data;
  }

  async complete(consultationId: string, request: SaveOpdConsultationRequest): Promise<OpdConsultationNote> {
    const response = await this.client.post<OpdConsultationNote>(API_ROUTES.opdConsultations.complete(consultationId), request);
    return response.data;
  }
}
