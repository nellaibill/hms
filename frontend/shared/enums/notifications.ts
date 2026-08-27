/** Mirrors HMS.Modules.Notifications.Contracts.NotificationChannel — serialized as strings
 * (JsonStringEnumConverter). */
export const NOTIFICATION_CHANNELS = ['InApp', 'Email', 'Sms'] as const;
export type NotificationChannel = (typeof NOTIFICATION_CHANNELS)[number];

/** Mirrors HMS.Modules.Notifications.Contracts.NotificationSeverity. */
export const NOTIFICATION_SEVERITIES = ['Normal', 'Emergency'] as const;
export type NotificationSeverity = (typeof NOTIFICATION_SEVERITIES)[number];
