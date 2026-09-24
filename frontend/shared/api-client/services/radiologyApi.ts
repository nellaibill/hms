import { API_ROUTES } from '../../constants';
import type { XrayAiAnalysisResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Radiology module, built on the shared HTTP client. Mirrors
 * HMS.Modules.Radiology.Endpoints.RadiologyAiController.
 */
export class RadiologyApi {
  constructor(private readonly client: HttpClient) {}

  /** Asks the configured vision model to read one stored patient image and saves the result as an
   * unreviewed draft. Can take a minute or more on a slow model, so callers should show progress. */
  async analyzeDocument(documentId: string, signal?: AbortSignal): Promise<XrayAiAnalysisResponse> {
    const response = await this.client.post<XrayAiAnalysisResponse>(API_ROUTES.radiology.aiAnalysis(documentId), undefined, { signal });
    return response.data;
  }

  /** Every saved analysis of the patient's images the caller can see, newest first. */
  async getPatientAnalyses(patientId: string): Promise<XrayAiAnalysisResponse[]> {
    const response = await this.client.get<XrayAiAnalysisResponse[]>(API_ROUTES.radiology.patientAnalyses(patientId));
    return response.data;
  }

  /** A doctor/radiologist confirms they have read a saved AI draft. Idempotent. */
  async markReviewed(analysisId: string): Promise<XrayAiAnalysisResponse> {
    const response = await this.client.post<XrayAiAnalysisResponse>(API_ROUTES.radiology.review(analysisId));
    return response.data;
  }
}
