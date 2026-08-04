/** Mirrors HMS.Modules.HR.Contracts.ShiftResponse. TimeOnly fields serialize as "HH:mm:ss". */
export interface Shift {
  id: string;
  code: string;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  graceMinutes: number;
  isNightShift: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateShiftRequest. */
export interface CreateShiftRequest {
  code: string;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  graceMinutes: number;
  isNightShift: boolean;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateShiftRequest — no Code, matching the backend
 * (Code is Shift's natural key, set only at creation). */
export interface UpdateShiftRequest {
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  graceMinutes: number;
  isNightShift: boolean;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.ShiftListQuery. */
export interface ShiftListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
