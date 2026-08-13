/** Mirrors HMS.Modules.IPD.Contracts.IPDDashboardResponse. */
export interface IpdDashboard {
  totalAdmitted: number;
  availableBeds: number;
  occupiedBeds: number;
  icuTotalBeds: number;
  icuOccupiedBeds: number;
  icuOccupancyRate: number;
  todaysAdmissions: number;
  todaysDischarges: number;
}
