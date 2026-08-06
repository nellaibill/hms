import { API_ROUTES } from '../../constants';
import type { CreateEventRequest, Event, EventListQuery, MonthlyEventQuery, UpdateEventRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedEvents {
  items: Event[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Calendar module, built on the shared HTTP client.
 * Feature code (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class EventsApi {
  constructor(private readonly client: HttpClient) {}

  async getEvents(query: EventListQuery = {}): Promise<PagedEvents> {
    const response = await this.client.get<Event[]>(API_ROUTES.events.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        eventType: query.eventType,
        departmentId: query.departmentId,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getEventsForMonth(query: MonthlyEventQuery): Promise<PagedEvents> {
    const response = await this.client.get<Event[]>(API_ROUTES.events.month, {
      query: {
        year: query.year,
        month: query.month,
        page: query.page,
        pageSize: query.pageSize,
        eventType: query.eventType,
        departmentId: query.departmentId,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getEventById(id: string): Promise<Event> {
    const response = await this.client.get<Event>(API_ROUTES.events.byId(id));
    return response.data;
  }

  async createEvent(request: CreateEventRequest): Promise<Event> {
    const response = await this.client.post<Event>(API_ROUTES.events.base, request);
    return response.data;
  }

  async updateEvent(id: string, request: UpdateEventRequest): Promise<Event> {
    const response = await this.client.put<Event>(API_ROUTES.events.byId(id), request);
    return response.data;
  }

  async deleteEvent(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.events.byId(id));
  }
}
