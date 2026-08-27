import type { NotificationSeverity } from '../../enums';

/** Mirrors HMS.Modules.Notifications.Contracts.NotificationResponse — one recipient's view
 * of a notification. `id` is the NotificationRecipient's id (what "mark as read" targets),
 * not the underlying Notification's own id (`notificationId`). */
export interface Notification {
  id: string;
  notificationId: string;
  templateKey: string;
  category: string;
  title: string;
  body: string;
  sourceModule: string;
  sourceEntityType?: string | null;
  sourceEntityId?: string | null;
  severity: NotificationSeverity;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

/** Mirrors HMS.Modules.Notifications.Contracts.NotificationListQuery. */
export interface NotificationListQuery {
  page?: number;
  pageSize?: number;
  isRead?: boolean;
}

/** Mirrors HMS.Modules.Notifications.Contracts.NotifyRequest — the admin manual-send shape. */
export interface NotifyRequest {
  templateKey: string;
  category: string;
  title: string;
  body?: string | null;
  placeholders?: Record<string, string> | null;
  sourceModule: string;
  sourceEntityType?: string | null;
  sourceEntityId?: string | null;
  severity: NotificationSeverity;
  recipientUserIds: string[];
}

/** Mirrors HMS.Modules.Notifications.Contracts.NotificationBroadcastResponse. */
export interface NotificationBroadcastResponse {
  notificationId: string;
  recipientCount: number;
}
