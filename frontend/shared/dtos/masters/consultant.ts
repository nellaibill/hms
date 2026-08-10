/** Mirrors HMS.Modules.Masters.Contracts.ConsultantResponse. */
export interface Consultant {
  id: string;
  code: string;
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateConsultantRequest. */
export interface CreateConsultantRequest {
  code: string;
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateConsultantRequest — no Code, matching the
 * backend (Code is Consultant's natural key, set only at creation). */
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
