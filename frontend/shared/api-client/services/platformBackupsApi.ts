import { API_ROUTES } from '../../constants';
import type { BackupSummaryResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the Platform module's backup surface — mirrors
 * HMS.Modules.Backups.Endpoints.PlatformBackupsController. Built on the platform-scoped
 * HttpClient instance (see PlatformHospitalsApi's own doc comment), never the hospital one.
 */
export class PlatformBackupsApi {
  constructor(private readonly client: HttpClient) {}

  /** Masters plus every active tenant's latest backup. */
  async getAll(): Promise<BackupSummaryResponse[]> {
    const response = await this.client.get<BackupSummaryResponse[]>(API_ROUTES.platformBackups.base);
    return response.data;
  }

  /** `key` is "masters" or a tenant id, as returned by getAll(). */
  async download(key: string): Promise<Blob> {
    return this.client.getBlob(API_ROUTES.platformBackups.download(key));
  }
}
