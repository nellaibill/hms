// The event categories this HMS instance's own built-in notification triggers use (see the
// Messaging & Notification Module design doc's event table) — not a backend-enforced
// catalog (NotificationPreferences.Category is a free-form string; a category with no saved
// preference row just uses the default, see NotificationPreference's own doc comment). Kept
// here so the Preferences screen has something to render a toggle row for before the user
// has ever received (and therefore implicitly discovered) a given category.
export const NOTIFICATION_CATEGORIES: { value: string; label: string }[] = [
  { value: 'appointment', label: 'Appointments' },
  { value: 'patient', label: 'Patient registration' },
  { value: 'billing', label: 'Billing & payments' },
  { value: 'diagnostics', label: 'Lab & diagnostics' },
  { value: 'task', label: 'Task assignments' },
  { value: 'message', label: 'New messages' },
  { value: 'emergency', label: 'Emergency alerts' },
];
