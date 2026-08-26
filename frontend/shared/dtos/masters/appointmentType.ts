/** Mirrors HMS.Modules.Masters.Contracts.AppointmentTypeResponse. */
export interface AppointmentType {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateAppointmentTypeRequest. */
export interface CreateAppointmentTypeRequest {
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateAppointmentTypeRequest. */
export interface UpdateAppointmentTypeRequest {
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.AppointmentTypeListQuery. */
export interface AppointmentTypeListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
