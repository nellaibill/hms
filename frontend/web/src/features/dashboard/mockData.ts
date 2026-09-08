/* -------------------------------- Statistics ------------------------------ */

export interface MonthlyCensusRow {
  month: string;
  op: number;
  ip: number;
}

export const monthlyPatientCensus: MonthlyCensusRow[] = [
  { month: 'Feb', op: 8120, ip: 1180 },
  { month: 'Mar', op: 8460, ip: 1245 },
  { month: 'Apr', op: 8290, ip: 1198 },
  { month: 'May', op: 8710, ip: 1302 },
  { month: 'Jun', op: 8580, ip: 1265 },
  { month: 'Jul', op: 9040, ip: 1340 },
];

/* -------------------------------- Finance ------------------------------- */

export interface DepartmentFinanceRow {
  department: string;
  income: number;
  expense: number;
}

// ₹ in lakhs, this month.
export const departmentFinance: DepartmentFinanceRow[] = [
  { department: 'Cardiology', income: 68, expense: 41 },
  { department: 'General Medicine', income: 54, expense: 36 },
  { department: 'Orthopedics', income: 47, expense: 30 },
  { department: 'Pediatrics', income: 38, expense: 27 },
  { department: 'Radiology', income: 33, expense: 22 },
  { department: 'ENT', income: 21, expense: 14 },
];

export interface MonthlyFinanceRow {
  month: string;
  revenue: number;
  expense: number;
}

// ₹ in lakhs.
export const revenueExpense: MonthlyFinanceRow[] = [
  { month: 'Feb', revenue: 186, expense: 142 },
  { month: 'Mar', revenue: 198, expense: 150 },
  { month: 'Apr', revenue: 205, expense: 148 },
  { month: 'May', revenue: 221, expense: 159 },
  { month: 'Jun', revenue: 214, expense: 162 },
  { month: 'Jul', revenue: 238, expense: 168 },
];

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

/* ----------------------------- Plans & Projects ---------------------------- */

export type DepartmentGoalStatus = 'On Track' | 'At Risk' | 'Delayed' | 'Completed';

export interface DepartmentGoal {
  id: string;
  department: string;
  goal: string;
  status: DepartmentGoalStatus;
  progress: number;
}

export const departmentGoals: DepartmentGoal[] = [
  { id: 'dg-1', department: 'Cardiology', goal: 'Reduce average OP wait time to under 20 minutes', status: 'On Track', progress: 72 },
  { id: 'dg-2', department: 'General Medicine', goal: 'Roll out digital case-sheets for all IP admissions', status: 'At Risk', progress: 45 },
  { id: 'dg-3', department: 'Radiology', goal: 'Commission the new MRI suite', status: 'Delayed', progress: 30 },
  { id: 'dg-4', department: 'Nursing', goal: 'Complete NABH nursing-protocol refresher training', status: 'Completed', progress: 100 },
  { id: 'dg-5', department: 'Pharmacy', goal: 'Cut prescription-to-dispense time to under 10 minutes', status: 'On Track', progress: 64 },
];
