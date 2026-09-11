import { API_ROUTES } from '../../constants';
import type { BackupSummaryResponse } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for a hospital's own backup — mirrors
 * HMS.Modules.Backups.Endpoints.TenantBackupsController. Built on the tenant-scoped
 * `httpClient`, never the Platform one — there is no way to name a different tenant here, by
 * design (see that controller's own doc comment).
 */
export class BackupsApi {
  constructor(private readonly client: HttpClient) {}

  async getMine(): Promise<BackupSummaryResponse> {
    const response = await this.client.get<BackupSummaryResponse>(API_ROUTES.backups.mine);
    return response.data;
  }

  async downloadMine(): Promise<Blob> {
    return this.client.getBlob(API_ROUTES.backups.mineDownload);
  }
}
