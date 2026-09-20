import { API_ROUTES } from '../../constants';
import type { ActivityLogDetail, ActivityLogEntry, ActivityLogListQuery } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedActivityLogs {
  items: ActivityLogEntry[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the read-only audit trail (HMS.Modules.ActivityLog.Endpoints.
 * ActivityLogsController), built on the shared HTTP client — docs/FrontendArchitecture.md §6.
 */
export class ActivityLogApi {
  constructor(private readonly client: HttpClient) {}

  async getActivityLogs(query: ActivityLogListQuery = {}): Promise<PagedActivityLogs> {
    const response = await this.client.get<ActivityLogEntry[]>(API_ROUTES.activityLogs.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        from: query.from,
        to: query.to,
        userId: query.userId,
        module: query.module,
        action: query.action,
        entityType: query.entityType,
        entityId: query.entityId,
        search: query.search,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getActivityLogById(id: string): Promise<ActivityLogDetail> {
    const response = await this.client.get<ActivityLogDetail>(API_ROUTES.activityLogs.byId(id));
    return response.data;
  }
}
