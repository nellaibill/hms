import { API_ROUTES } from '../../constants';
import type { PagedQuery, PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

/** Any camelCase entity key from API_ROUTES.masters (brand, currency, storageLocation, ...). */
export type MastersEntityKey = keyof typeof API_ROUTES.masters;

export interface MastersListQuery extends PagedQuery {
  isActive?: boolean;
  /** Extra entity-specific filters (e.g. { departmentId }) forwarded as-is to
   * the real per-entity REST endpoint (see MasterEntityConfig.listFilters on the frontend) —
   * generic so a new filterable field never needs a change here. */
  filters?: Record<string, string | undefined>;
}

/** A Masters record's shape varies per entity — described by each MasterEntityConfig.fields on the frontend. */
export type MasterRecordDto = Record<string, unknown>;

export interface PagedMasterRecords {
  items: MasterRecordDto[];
  meta: PaginationMeta;
}

/**
 * Typed API service for Masters (Reference Data), built on the shared HTTP client.
 * Parameterized by entityKey rather than one class per entity — the 16 Masters controllers
 * (HMS.Modules.Masters.Endpoints.*Controller) all share the same GET/POST/GET{id}/PUT{id}
 * shape, so a generic client avoids 16 near-duplicate classes. Per-field typing already
 * lives in each entity's MasterEntityConfig on the frontend (features/masters/configs/*).
 */
export class MastersApi {
  constructor(private readonly client: HttpClient) {}

  private routes(entityKey: MastersEntityKey) {
    return API_ROUTES.masters[entityKey];
  }

  async list(entityKey: MastersEntityKey, query: MastersListQuery = {}): Promise<PagedMasterRecords> {
    const response = await this.client.get<MasterRecordDto[]>(this.routes(entityKey).base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
        ...query.filters,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getById(entityKey: MastersEntityKey, id: string): Promise<MasterRecordDto> {
    const response = await this.client.get<MasterRecordDto>(this.routes(entityKey).byId(id));
    return response.data;
  }

  async create(entityKey: MastersEntityKey, payload: MasterRecordDto): Promise<MasterRecordDto> {
    const response = await this.client.post<MasterRecordDto>(this.routes(entityKey).base, payload);
    return response.data;
  }

  async update(entityKey: MastersEntityKey, id: string, payload: MasterRecordDto): Promise<MasterRecordDto> {
    const response = await this.client.put<MasterRecordDto>(this.routes(entityKey).byId(id), payload);
    return response.data;
  }

  /** Uploads (or replaces) a record's photo — only entities with a photo field (see
   * MasterEntityConfig.photo on the frontend) actually expose a `{id}/photo` endpoint; calling
   * this for any other entity 404s. Mirrors UsersApi.uploadProfilePhoto's exact multipart shape. */
  async uploadPhoto(entityKey: MastersEntityKey, id: string, file: File): Promise<MasterRecordDto> {
    const formData = new FormData();
    // Field name must be "photo" — mirrors {Entity}Controller.UploadPhoto's IFormFile
    // parameter name, which ASP.NET Core model binding matches against.
    formData.append('photo', file);
    const response = await this.client.postFormData<MasterRecordDto>(`${this.routes(entityKey).byId(id)}/photo`, formData);
    return response.data;
  }
}
