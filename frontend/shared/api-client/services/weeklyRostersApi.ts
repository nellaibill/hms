import { API_ROUTES } from '../../constants';
import type {
  CopyWeeklyRosterRequest,
  CreateWeeklyRosterRequest,
  MonthlyWeeklyRosterQuery,
  UpdateWeeklyRosterRequest,
  WeeklyRoster,
  WeeklyRosterListQuery,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedWeeklyRosters {
  items: WeeklyRoster[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Weekly Roster module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class WeeklyRostersApi {
  constructor(private readonly client: HttpClient) {}

  async getWeeklyRosters(query: WeeklyRosterListQuery = {}): Promise<PagedWeeklyRosters> {
    const response = await this.client.get<WeeklyRoster[]>(API_ROUTES.weeklyRosters.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getWeeklyRosterById(id: string): Promise<WeeklyRoster> {
    const response = await this.client.get<WeeklyRoster>(API_ROUTES.weeklyRosters.byId(id));
    return response.data;
  }

  async createWeeklyRoster(request: CreateWeeklyRosterRequest): Promise<WeeklyRoster> {
    const response = await this.client.post<WeeklyRoster>(API_ROUTES.weeklyRosters.base, request);
    return response.data;
  }

  async updateWeeklyRoster(id: string, request: UpdateWeeklyRosterRequest): Promise<WeeklyRoster> {
    const response = await this.client.put<WeeklyRoster>(API_ROUTES.weeklyRosters.byId(id), request);
    return response.data;
  }

  async deleteWeeklyRoster(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.weeklyRosters.byId(id));
  }

  /** Idempotent per the backend's own doc comment — publishing an already-published roster is a no-op, not an error. */
  async publishWeeklyRoster(id: string): Promise<WeeklyRoster> {
    const response = await this.client.post<WeeklyRoster>(API_ROUTES.weeklyRosters.publish(id));
    return response.data;
  }

  /** Duplicates the roster's metadata (DepartmentId) onto a new, unpublished roster for the
   * caller-chosen target week. Does not copy ShiftAssignments. */
  async copyWeeklyRoster(id: string, request: CopyWeeklyRosterRequest): Promise<WeeklyRoster> {
    const response = await this.client.post<WeeklyRoster>(API_ROUTES.weeklyRosters.copy(id), request);
    return response.data;
  }

  /** Rosters whose WeekStartDate falls within the given calendar month — a read-only view
   * over this same aggregate, not a separate resource. */
  async getMonthlyWeeklyRosters(query: MonthlyWeeklyRosterQuery): Promise<PagedWeeklyRosters> {
    const response = await this.client.get<WeeklyRoster[]>(API_ROUTES.weeklyRosters.monthly, {
      query: {
        year: query.year,
        month: query.month,
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }
}
