import type { AvailabilityStatus } from '../../enums/hr';

/** Mirrors HMS.Modules.HR.Contracts.StaffAvailabilityResponse. */
export interface StaffAvailability {
  id: string;
  staffId: string;
  startDate: string;
  endDate: string;
  availabilityStatus: AvailabilityStatus;
  reason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateStaffAvailabilityRequest. */
export interface CreateStaffAvailabilityRequest {
  staffId: string;
  startDate: string;
  endDate: string;
  availabilityStatus: AvailabilityStatus;
  reason?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateStaffAvailabilityRequest. */
export interface UpdateStaffAvailabilityRequest {
  staffId: string;
  startDate: string;
  endDate: string;
  availabilityStatus: AvailabilityStatus;
  reason?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.StaffAvailabilityListQuery. */
export interface StaffAvailabilityListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
}
