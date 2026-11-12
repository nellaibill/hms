import { API_ROUTES } from '../../constants';
import type {
  SyncActionResponse,
  SyncDashboardSession,
  SyncEventListQuery,
  SyncEventRow,
  SyncResolveRequest,
  SyncStreamListQuery,
  SyncStreamRow,
  SyncSummary,
} from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedSyncStreams {
  items: SyncStreamRow[];
  meta: PaginationMeta;
}

export interface PagedSyncEvents {
  items: SyncEventRow[];
  meta: PaginationMeta;
}

/**
 * Typed client for the offline-first cloud sync monitoring API
 * (HMS.Modules.Sync.Endpoints.SyncMonitorController, ADR-133). Talks only to this server's API:
 * the browser never reaches the cloud or the outbox tables directly.
 */
export class SyncMonitorApi {
  constructor(private readonly client: HttpClient) {}

  async getSummary(): Promise<SyncSummary> {
    return (await this.client.get<SyncSummary>(API_ROUTES.syncMonitor.summary)).data;
  }

  async getStreams(query: SyncStreamListQuery = {}): Promise<PagedSyncStreams> {
    const response = await this.client.get<SyncStreamRow[]>(API_ROUTES.syncMonitor.streams, {
      query: { page: query.page, pageSize: query.pageSize, search: query.search, status: query.status },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getPatientHistory(patientId: string): Promise<SyncEventRow[]> {
    return (await this.client.get<SyncEventRow[]>(API_ROUTES.syncMonitor.patientEvents(patientId))).data;
  }

  async getEvents(query: SyncEventListQuery = {}): Promise<PagedSyncEvents> {
    const response = await this.client.get<SyncEventRow[]>(API_ROUTES.syncMonitor.events, {
      query: { page: query.page, pageSize: query.pageSize, status: query.status },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async retryEvent(eventId: string): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.retryEvent(eventId))).data;
  }

  async retryAll(): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.retryAll)).data;
  }

  async resolveEvent(eventId: string, request: SyncResolveRequest): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.resolveEvent(eventId), request)).data;
  }

  async pause(): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.pause)).data;
  }

  async resume(): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.resume)).data;
  }

  async trigger(): Promise<SyncActionResponse> {
    return (await this.client.post<SyncActionResponse>(API_ROUTES.syncMonitor.trigger)).data;
  }

  async createDashboardSession(): Promise<SyncDashboardSession> {
    return (await this.client.post<SyncDashboardSession>(API_ROUTES.syncMonitor.dashboardSession)).data;
  }
}
