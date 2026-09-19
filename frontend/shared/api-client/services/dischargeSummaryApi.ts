import { API_ROUTES } from '../../constants';
import type {
  DischargeSummary,
  DischargeSummaryDraftSuggestion,
  FinalizeDischargeSummaryRequest,
  UpdateDischargeSummaryRequest,
} from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for Discharge Summaries, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 * Mirrors AdmissionsApi's shape (constructor + one method per endpoint returning an
 * already-typed DTO).
 */
export class DischargeSummaryApi {
  constructor(private readonly client: HttpClient) {}

  /** Creates the Draft discharge summary for an already-discharged admission. 409 if one
   * already exists for this admission; a domain error if the admission isn't Discharged yet. */
  async createDraft(admissionId: string): Promise<DischargeSummary> {
    const response = await this.client.post<DischargeSummary>(API_ROUTES.dischargeSummaries.byAdmissionId(admissionId));
    return response.data;
  }

  /** Fetches the existing discharge summary for an admission. 404 (ApiError) if none has been created yet. */
  async getByAdmissionId(admissionId: string): Promise<DischargeSummary> {
    const response = await this.client.get<DischargeSummary>(API_ROUTES.dischargeSummaries.byAdmissionId(admissionId));
    return response.data;
  }

  async getById(id: string): Promise<DischargeSummary> {
    const response = await this.client.get<DischargeSummary>(API_ROUTES.dischargeSummaries.byId(id));
    return response.data;
  }

  /** Only succeeds while Status is Draft; rejected once Finalized. */
  async update(id: string, request: UpdateDischargeSummaryRequest): Promise<DischargeSummary> {
    const response = await this.client.put<DischargeSummary>(API_ROUTES.dischargeSummaries.byId(id), request);
    return response.data;
  }

  /** AI-drafts narrative fields (plus vitals from the last IPD reading) for a Draft summary. A
   * suggestion only — nothing is saved; the caller merges it into the form and saves via update. */
  async aiDraft(id: string): Promise<DischargeSummaryDraftSuggestion> {
    const response = await this.client.post<DischargeSummaryDraftSuggestion>(API_ROUTES.dischargeSummaries.aiDraft(id), {});
    return response.data;
  }

  /** Locks the record — irreversible. */
  async finalize(id: string, request: FinalizeDischargeSummaryRequest = {}): Promise<DischargeSummary> {
    const response = await this.client.post<DischargeSummary>(API_ROUTES.dischargeSummaries.finalize(id), request);
    return response.data;
  }
}
