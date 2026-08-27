import { API_ROUTES } from '../../constants';
import type { Notification, NotificationBroadcastResponse, NotificationListQuery, NotifyRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedNotifications {
  items: Notification[];
  meta: PaginationMeta;
}

/**
 * Typed API service for "my notifications" (HMS.Modules.Notifications.Endpoints.
 * NotificationsController), built on the shared HTTP client — docs/FrontendArchitecture.md §6.
 */
export class NotificationsApi {
  constructor(private readonly client: HttpClient) {}

  async getMine(query: NotificationListQuery = {}): Promise<PagedNotifications> {
    const response = await this.client.get<Notification[]>(API_ROUTES.notifications.base, {
      query: { page: query.page, pageSize: query.pageSize, isRead: query.isRead },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getUnreadCount(): Promise<number> {
    const response = await this.client.get<{ count: number }>(API_ROUTES.notifications.unreadCount);
    return response.data.count;
  }

  async markAsRead(id: string): Promise<void> {
    await this.client.put(API_ROUTES.notifications.markRead(id));
  }

  async markAllAsRead(): Promise<void> {
    await this.client.put(API_ROUTES.notifications.markAllRead);
  }

  /** The admin manual-send action (e.g. an emergency broadcast) — most notifications are
   * raised by another module calling INotificationService in-process, not through this. */
  async notify(request: NotifyRequest): Promise<NotificationBroadcastResponse> {
    const response = await this.client.post<NotificationBroadcastResponse>(API_ROUTES.notifications.base, request);
    return response.data;
  }
}
