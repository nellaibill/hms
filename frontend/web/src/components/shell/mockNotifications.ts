export interface MockNotification {
  id: string;
  title: string;
  detail: string;
  time: string;
  severity: 'info' | 'warning' | 'critical';
}

export const mockNotifications: MockNotification[] = [
  { id: 'n-1', title: 'Critical lab value', detail: 'Potassium result flagged for UHID 000482', time: '4 min ago', severity: 'critical' },
  { id: 'n-2', title: 'Discount approval requested', detail: 'Reception requested a 15% discount on Invoice #2291', time: '22 min ago', severity: 'warning' },
  { id: 'n-3', title: 'Roster published', detail: 'Next week\'s nursing roster is now live', time: '1 hr ago', severity: 'info' },
];
