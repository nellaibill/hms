/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticPackageItemResponse. */
export interface DiagnosticPackageItem {
  id: string;
  serviceId: string;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticPackageResponse. See diagnosticCategory.ts
 * for why this lives outside dtos/masters. */
export interface DiagnosticPackage {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  /** A deliberate bundle-discount price — independent of the sum of the package's item
   * prices, never auto-computed. */
  totalPrice: number;
  isActive: boolean;
  items: DiagnosticPackageItem[];
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDiagnosticPackageRequest. */
export interface CreateDiagnosticPackageRequest {
  code: string;
  name: string;
  description?: string | null;
  totalPrice: number;
  isActive: boolean;
  /** At least one DiagnosticService id is required. */
  serviceIds: string[];
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDiagnosticPackageRequest — items are NOT
 * editable here, only via AddDiagnosticPackageItemRequest / the remove-item endpoint. */
export interface UpdateDiagnosticPackageRequest {
  code: string;
  name: string;
  description?: string | null;
  totalPrice: number;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.AddDiagnosticPackageItemRequest. */
export interface AddDiagnosticPackageItemRequest {
  serviceId: string;
}

/** Mirrors HMS.Modules.Masters.Contracts.DiagnosticPackageListQuery. */
export interface DiagnosticPackageListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
