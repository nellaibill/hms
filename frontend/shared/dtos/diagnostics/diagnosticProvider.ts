/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticProviderResponse — the "External Lab" from
 * Central Laboratory's UI, named generically since it also covers external imaging centers
 * for Radiology. See diagnosticCategory.ts for why this lives outside dtos/masters. */
export interface DiagnosticProvider {
  id: string;
  code: string;
  name: string;
  contactDetails?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDiagnosticProviderRequest. */
export interface CreateDiagnosticProviderRequest {
  code: string;
  name: string;
  contactDetails?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDiagnosticProviderRequest. */
export interface UpdateDiagnosticProviderRequest {
  code: string;
  name: string;
  contactDetails?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticProviderListQuery. */
export interface DiagnosticProviderListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
