/** Only these two service types are accepted on DiagnosticService (the new, typed catalog) —
 * Procedure stays exclusively on the old, untyped DiagnosticTest master. Mirrors the subset of
 * HMS.Modules.Masters.Contracts' DiagnosticTestServiceType enforced by
 * CreateDiagnosticServiceRequestValidator/UpdateDiagnosticServiceRequestValidator. */
export const DIAGNOSTIC_SERVICE_TYPES = ['Laboratory', 'Radiology'] as const;
export type DiagnosticServiceType = (typeof DIAGNOSTIC_SERVICE_TYPES)[number];

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticServiceResponse. See diagnosticCategory.ts
 * for why this lives outside dtos/masters. */
export interface DiagnosticService {
  id: string;
  code: string;
  name: string;
  categoryId: string;
  serviceType: DiagnosticServiceType;
  isOutsourced: boolean;
  providerId?: string | null;
  price: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDiagnosticServiceRequest. */
export interface CreateDiagnosticServiceRequest {
  code: string;
  name: string;
  categoryId: string;
  serviceType: DiagnosticServiceType;
  isOutsourced: boolean;
  /** Required by the backend validator when isOutsourced is true. */
  providerId?: string | null;
  price: number;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDiagnosticServiceRequest. */
export interface UpdateDiagnosticServiceRequest {
  code: string;
  name: string;
  categoryId: string;
  serviceType: DiagnosticServiceType;
  isOutsourced: boolean;
  providerId?: string | null;
  price: number;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticServiceListQuery. */
export interface DiagnosticServiceListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  categoryId?: string;
  serviceType?: DiagnosticServiceType;
  isOutsourced?: boolean;
  isActive?: boolean;
}
