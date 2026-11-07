/** Mirrors HMS.Modules.Masters.Contracts.ConsultationTypeResponse. */
export interface ConsultationType {
  id: string;
  name: string;
  /** Optional short label reports show instead of name (e.g. "Regular"), ADR-127. */
  shortName?: string | null;
  /** Standard fee for this consultation category — null when there's no fixed rate (e.g.
   * "Others / On-call," decided per-visit instead). */
  amount?: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateConsultationTypeRequest. */
export interface CreateConsultationTypeRequest {
  name: string;
  shortName?: string | null;
  amount?: number | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateConsultationTypeRequest. */
export interface UpdateConsultationTypeRequest {
  name: string;
  shortName?: string | null;
  amount?: number | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.ConsultationTypeListQuery. */
export interface ConsultationTypeListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
