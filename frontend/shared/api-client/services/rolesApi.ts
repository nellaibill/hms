import { API_ROUTES } from '../../constants';
import type { CreateRoleRequest, RoleListQuery, RoleResponse, UpdateRoleRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedRoles {
  items: RoleResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Roles module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 *
 * RolesController.GetPaged doesn't yet follow the `{ data: T[], meta }` convention every
 * other list endpoint (Users, Patients) uses — it nests the whole PagedResult (items plus
 * paging fields) under `data` with no top-level `meta` at all. getRoles() unwraps that
 * shape as-is so the client works against the API that actually exists today; if
 * RolesController is ever normalized to match the other modules, only the destructuring
 * below needs to change; the PagedRoles the caller sees stays the same either way.
 */
export class RolesApi {
  constructor(private readonly client: HttpClient) {}

  async getRoles(query: RoleListQuery = {}): Promise<PagedRoles> {
    const response = await this.client.get<{
      items: RoleResponse[];
      page: number;
      pageSize: number;
      totalCount: number;
      totalPages: number;
    }>(API_ROUTES.roles.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
      },
    });

    return {
      items: response.data.items,
      meta: {
        page: response.data.page,
        pageSize: response.data.pageSize,
        totalCount: response.data.totalCount,
        totalPages: response.data.totalPages,
      },
    };
  }

  async getRoleById(id: string): Promise<RoleResponse> {
    const response = await this.client.get<RoleResponse>(API_ROUTES.roles.byId(id));
    return response.data;
  }

  async createRole(request: CreateRoleRequest): Promise<RoleResponse> {
    const response = await this.client.post<RoleResponse>(API_ROUTES.roles.base, request);
    return response.data;
  }

  async updateRole(id: string, request: UpdateRoleRequest): Promise<RoleResponse> {
    const response = await this.client.put<RoleResponse>(API_ROUTES.roles.byId(id), request);
    return response.data;
  }

  async activateRole(id: string): Promise<RoleResponse> {
    const response = await this.client.post<RoleResponse>(API_ROUTES.roles.activate(id));
    return response.data;
  }

  async deactivateRole(id: string): Promise<RoleResponse> {
    const response = await this.client.post<RoleResponse>(API_ROUTES.roles.deactivate(id));
    return response.data;
  }
}
