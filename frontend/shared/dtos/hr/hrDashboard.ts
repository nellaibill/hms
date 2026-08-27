/**
 * Mirrors HMS.Modules.HR.Contracts.HrDashboardResponse — GET /api/v1/hr/dashboard's response
 * shape. presentToday folds Late and HalfDay attendance rows into the headline "present"
 * count (a Late/HalfDay employee did still show up); absentToday/onLeaveToday are each their
 * own exact Attendance.Status count for the current UTC calendar date.
 */
export interface HrDashboardResponse {
  totalEmployees: number;
  activeEmployees: number;
  presentToday: number;
  absentToday: number;
  onLeaveToday: number;
  pendingLeaveRequests: number;
  expiringDocuments: number;
}
