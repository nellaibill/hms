/** Mirrors HMS.Modules.Masters.Contracts.DiagnosisResponse. */
export interface Diagnosis {
  id: string;
  name: string;
  icdCode?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDiagnosisRequest. */
export interface CreateDiagnosisRequest {
  name: string;
  icdCode?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDiagnosisRequest. */
export interface UpdateDiagnosisRequest {
  name: string;
  icdCode?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosisListQuery. */
export interface DiagnosisListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
