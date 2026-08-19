import { API_ROUTES } from '../../constants';
import type {
  CreateHospitalRequest,
  CreateHospitalResponse,
  DeletedTenantListItemResponse,
  TenantDashboardStatsResponse,
  TenantDeletePreviewResponse,
  TenantListItemResponse,
  TenantListQuery,
  UpdateTenantStatusRequest,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedTenants {
  items: TenantListItemResponse[];
  meta: PaginationMeta;
}

export interface PagedDeletedTenants {
  items: DeletedTenantListItemResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Platform module's hospital registration/provisioning surface.
 * Built on the same platform-scoped HttpClient instance as PlatformAuthApi
 * (frontend/web/src/services/apiClient.ts) — never the hospital-scoped one.
 */
export class PlatformHospitalsApi {
  constructor(private readonly client: HttpClient) {}

  /**
   * `idempotencyKey` must stay the same across retries of one logical registration attempt
   * (e.g. after a client-side timeout) — the backend uses it to avoid double-provisioning a
   * hospital database. Callers should generate a fresh key only when starting a genuinely
   * new registration, not on every call.
   */
  async createHospital(request: CreateHospitalRequest, idempotencyKey: string): Promise<CreateHospitalResponse> {
    const response = await this.client.post<CreateHospitalResponse>(API_ROUTES.platformHospitals.base, request, {
      headers: { 'Idempotency-Key': idempotencyKey },
    });
    return response.data;
  }

  async getHospitals(query: TenantListQuery = {}): Promise<PagedTenants> {
    const response = await this.client.get<TenantListItemResponse[]>(API_ROUTES.platformHospitals.base, {
      query: { page: query.page, pageSize: query.pageSize, search: query.search },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getStats(): Promise<TenantDashboardStatsResponse> {
    const response = await this.client.get<TenantDashboardStatsResponse>(API_ROUTES.platformHospitals.stats);
    return response.data;
  }

  async updateStatus(id: string, request: UpdateTenantStatusRequest): Promise<TenantListItemResponse> {
    const response = await this.client.patch<TenantListItemResponse>(API_ROUTES.platformHospitals.status(id), request);
    return response.data;
  }

  async getDeletedHospitals(query: TenantListQuery = {}): Promise<PagedDeletedTenants> {
    const response = await this.client.get<DeletedTenantListItemResponse[]>(API_ROUTES.platformHospitals.deleted, {
      query: { page: query.page, pageSize: query.pageSize, search: query.search },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getDeletePreview(id: string): Promise<TenantDeletePreviewResponse> {
    const response = await this.client.get<TenantDeletePreviewResponse>(API_ROUTES.platformHospitals.deletePreview(id));
    return response.data;
  }

  /** Soft-deletes a hospital — its own database is never touched, and it can be restored
   * via `restoreHospital`. `confirmHospitalCode` must match the hospital's actual code
   * (server-enforced). */
  async deleteHospital(id: string, confirmHospitalCode: string): Promise<void> {
    await this.client.delete<void>(API_ROUTES.platformHospitals.byId(id), { query: { confirmHospitalCode } });
  }

  async restoreHospital(id: string): Promise<TenantListItemResponse> {
    const response = await this.client.post<TenantListItemResponse>(API_ROUTES.platformHospitals.restore(id));
    return response.data;
  }
}
