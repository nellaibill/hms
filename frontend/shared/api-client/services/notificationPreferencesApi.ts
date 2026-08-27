import { API_ROUTES } from '../../constants';
import type { NotificationPreference, UpdateNotificationPreferenceRequest } from '../../dtos';
import type { HttpClient } from '../httpClient';

/**
 * Typed API service for the caller's own notification channel preferences
 * (HMS.Modules.Notifications.Endpoints.NotificationPreferencesController).
 */
export class NotificationPreferencesApi {
  constructor(private readonly client: HttpClient) {}

  async getMine(): Promise<NotificationPreference[]> {
    const response = await this.client.get<NotificationPreference[]>(API_ROUTES.notificationPreferences);
    return response.data;
  }

  async upsertMine(request: UpdateNotificationPreferenceRequest): Promise<NotificationPreference> {
    const response = await this.client.put<NotificationPreference>(API_ROUTES.notificationPreferences, request);
    return response.data;
  }
}
