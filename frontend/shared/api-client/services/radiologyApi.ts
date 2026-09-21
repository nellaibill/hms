import { API_ROUTES } from '../../constants';
import type { XrayAiAnalysisResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Radiology module, built on the shared HTTP client. Mirrors
 * HMS.Modules.Radiology.Endpoints.RadiologyAiController.
 */
export class RadiologyApi {
  constructor(private readonly client: HttpClient) {}

  /** Asks the configured vision model to read one stored image document. Can take a minute or
   * more on a CPU-hosted model, so callers should show progress rather than expect a fast reply. */
  async analyzeDocument(documentId: string, signal?: AbortSignal): Promise<XrayAiAnalysisResponse> {
    const response = await this.client.post<XrayAiAnalysisResponse>(API_ROUTES.radiology.aiAnalysis(documentId), undefined, { signal });
    return response.data;
  }
}
