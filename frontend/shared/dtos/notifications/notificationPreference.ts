/** Mirrors HMS.Modules.Notifications.Contracts.NotificationPreferenceResponse. */
export interface NotificationPreference {
  id: string;
  category: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
  smsEnabled: boolean;
}

/** Mirrors HMS.Modules.Notifications.Contracts.UpdateNotificationPreferenceRequest — an
 * upsert for one category at a time. */
export interface UpdateNotificationPreferenceRequest {
  category: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
  smsEnabled: boolean;
}
