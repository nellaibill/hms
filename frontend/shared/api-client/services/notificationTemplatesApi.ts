import { API_ROUTES } from '../../constants';
import type { CreateNotificationTemplateRequest, NotificationTemplate, UpdateNotificationTemplateRequest } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the admin notification-template editor
 * (HMS.Modules.Notifications.Endpoints.NotificationTemplatesController).
 */
export class NotificationTemplatesApi {
  constructor(private readonly client: HttpClient) {}

  async getAll(isActive?: boolean): Promise<NotificationTemplate[]> {
    const response = await this.client.get<NotificationTemplate[]>(API_ROUTES.notificationTemplates.base, { query: { isActive } });
    return response.data;
  }

  async create(request: CreateNotificationTemplateRequest): Promise<NotificationTemplate> {
    const response = await this.client.post<NotificationTemplate>(API_ROUTES.notificationTemplates.base, request);
    return response.data;
  }

  async update(id: string, request: UpdateNotificationTemplateRequest): Promise<NotificationTemplate> {
    const response = await this.client.put<NotificationTemplate>(API_ROUTES.notificationTemplates.byId(id), request);
    return response.data;
  }
}
