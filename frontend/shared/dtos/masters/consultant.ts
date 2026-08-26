/** Mirrors HMS.Modules.Masters.Contracts.ConsultantResponse. */
export interface Consultant {
  id: string;
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateConsultantRequest. */
export interface CreateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateConsultantRequest. */
export interface UpdateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.ConsultantListQuery. */
export interface ConsultantListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
  departmentId?: string;
}
