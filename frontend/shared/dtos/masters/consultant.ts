/** Mirrors HMS.Modules.Masters.Contracts.ConsultantType. */
export type ConsultantType = 'InHouse' | 'Visiting';

/** Mirrors HMS.Modules.Masters.Contracts.ConsultantResponse. */
export interface Consultant {
  id: string;
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  consultantType?: ConsultantType | null;
  /** Day names, e.g. "Monday" — see Domain/Consultant.cs's own doc comment. */
  availableDays: string[];
  /** TimeOnly serializes as "HH:mm:ss". */
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateConsultantRequest. */
export interface CreateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  consultantType?: ConsultantType | null;
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateConsultantRequest. */
export interface UpdateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  consultantType?: ConsultantType | null;
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
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
