/** Mirrors HMS.Modules.Masters.Contracts.ConsultantResponse. */
export interface Consultant {
  id: string;
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  /** Relative path (e.g. "uploads/Tenant/{tenantId}/consultants/{id}.jpg"), set only via the dedicated photo
   * upload endpoint — resolve to a full URL the same way Consultant Photo does elsewhere
   * (`${apiBaseUrl}/${photoUrl}`). */
  photoUrl?: string | null;
  /** Day names, e.g. "Monday" — see Domain/Consultant.cs's own doc comment. */
  availableDays: string[];
  /** TimeOnly serializes as "HH:mm:ss". */
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  /** Optional second visiting session on the same days. */
  visitStartTime2?: string | null;
  visitEndTime2?: string | null;
  /** Masters ConsultationType ids this consultant offers, paired with what the hospital pays
   * them for each (nullable — a rate not yet decided). */
  consultationTypeCharges: ConsultationTypeCharge[];
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.ConsultationTypeChargeDto. */
export interface ConsultationTypeCharge {
  consultationTypeId: string;
  consultantCharge?: number | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateConsultantRequest. */
export interface CreateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  /** Optional second visiting session on the same days. */
  visitStartTime2?: string | null;
  visitEndTime2?: string | null;
  consultationTypeCharges: ConsultationTypeCharge[];
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateConsultantRequest. */
export interface UpdateConsultantRequest {
  name: string;
  departmentId?: string | null;
  specialization?: string | null;
  isActive: boolean;
  priority?: number | null;
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  /** Optional second visiting session on the same days. */
  visitStartTime2?: string | null;
  visitEndTime2?: string | null;
  consultationTypeCharges: ConsultationTypeCharge[];
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
