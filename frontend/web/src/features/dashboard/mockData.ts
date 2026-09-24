/* Dashboard placeholder data that has no backend yet. The Executive Dashboard's charts now read
   real data (regression report DASH-01); only the header's Pending Tasks list still uses this. */

/* Reused by the header's Pending Tasks menu (components/shell/PendingTasksMenu.tsx) so the
   two stay in sync — this array has no dashboard card of its own anymore. */
export interface PendingTask {
  id: string;
  title: string;
  due: string;
  priority: 'Low' | 'Medium' | 'High';
}

export const pendingTasks: PendingTask[] = [
  { id: 'pt-1', title: 'Approve pharmacy purchase order #PO-448', due: 'Today, 4:00 PM', priority: 'High' },
  { id: 'pt-2', title: 'Review discount request — Invoice #2291', due: 'Today, 5:30 PM', priority: 'Medium' },
  { id: 'pt-3', title: 'Sign off nursing roster — Ward 3', due: 'Tomorrow', priority: 'Medium' },
  { id: 'pt-4', title: 'Renew Dr. Senthil Kumar’s license record', due: 'In 3 days', priority: 'Low' },
];
