/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticCategoryResponse. Deliberately kept out of
 * dtos/masters (unlike the untyped MasterRecordDto entities there) — this entity has bespoke
 * UI (Central Laboratory's Categories screen), so it gets a proper typed DTO instead. */
export interface DiagnosticCategory {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDiagnosticCategoryRequest. */
export interface CreateDiagnosticCategoryRequest {
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDiagnosticCategoryRequest. */
export interface UpdateDiagnosticCategoryRequest {
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticCategoryListQuery. */
export interface DiagnosticCategoryListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
