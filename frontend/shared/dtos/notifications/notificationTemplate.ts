import type { NotificationChannel } from '../../enums';

/** Mirrors HMS.Modules.Notifications.Contracts.NotificationTemplateResponse. */
export interface NotificationTemplate {
  id: string;
  templateKey: string;
  channel: NotificationChannel;
  subject?: string | null;
  bodyTemplate: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Notifications.Contracts.CreateNotificationTemplateRequest. */
export interface CreateNotificationTemplateRequest {
  templateKey: string;
  channel: NotificationChannel;
  subject?: string | null;
  bodyTemplate: string;
}

/** Mirrors HMS.Modules.Notifications.Contracts.UpdateNotificationTemplateRequest. */
export interface UpdateNotificationTemplateRequest {
  subject?: string | null;
  bodyTemplate: string;
  isActive: boolean;
}
