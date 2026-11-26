/** Only these two service types are accepted on DiagnosticService (the new, typed catalog) —
 * Procedure stays exclusively on the old, untyped DiagnosticTest master. Mirrors the subset of
 * HMS.Modules.Masters.Contracts' DiagnosticTestServiceType enforced by
 * CreateDiagnosticServiceRequestValidator/UpdateDiagnosticServiceRequestValidator. */
export const DIAGNOSTIC_SERVICE_TYPES = ['Laboratory', 'Radiology'] as const;
export type DiagnosticServiceType = (typeof DIAGNOSTIC_SERVICE_TYPES)[number];

/** The samples a lab test can be listed under, in the tariff sheet's order (ADR-154) — mirrors
 * HMS.Modules.Masters.Contracts.DiagnosticSampleTypes. The Price List heads its lab tabs with
 * them ("In-house tests – Blood"). */
export const DIAGNOSTIC_SAMPLE_TYPES = ['Blood', 'Urine', 'Stool', 'Semen', 'Body Fluids', 'Histopathology'] as const;
export type DiagnosticSampleType = (typeof DIAGNOSTIC_SAMPLE_TYPES)[number];

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
  costPrice: number;
  isActive: boolean;
  /** The reporting doctor's share of `price` — Radiology only, always 0 for a lab test (ADR-140). */
  doctorCharge: number;
  /** What a bedside scan adds to the price — null when the service isn't done at the bedside. */
  bedsidePrice?: number | null;
  /** What a bedside scan adds to the doctor's share — null exactly when bedsidePrice is. */
  bedsideDoctorCharge?: number | null;
  /** Named alternative rates (e.g. "LH", "MR") billed instead of the standard one. Radiology only. */
  rateOptions: DiagnosticServiceRateOption[];
  /** What a lab test is run on (Blood, Urine…) — null for radiology or a test not placed (ADR-154). */
  sampleType?: string | null;
  /** Where it sits in the Price List — lower first; null when not placed yet (ADR-154). */
  sortOrder?: number | null;
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
  costPrice: number;
  isActive: boolean;
  /** The reporting doctor's share of `price` — Radiology only, always 0 for a lab test (ADR-140). */
  doctorCharge?: number;
  /** What a bedside scan adds to the price — null when the service isn't done at the bedside. */
  bedsidePrice?: number | null;
  /** What a bedside scan adds to the doctor's share — null exactly when bedsidePrice is. */
  bedsideDoctorCharge?: number | null;
  /** Named alternative rates (e.g. "LH", "MR") billed instead of the standard one. Radiology only. */
  rateOptions?: DiagnosticServiceRateOption[];
  /** What a lab test is run on (Blood, Urine…) — null for radiology or a test not placed (ADR-154). */
  sampleType?: string | null;
  /** Where it sits in the Price List — lower first; null when not placed yet (ADR-154). */
  sortOrder?: number | null;
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
  costPrice: number;
  isActive: boolean;
  /** The reporting doctor's share of `price` — Radiology only, always 0 for a lab test (ADR-140). */
  doctorCharge?: number;
  /** What a bedside scan adds to the price — null when the service isn't done at the bedside. */
  bedsidePrice?: number | null;
  /** What a bedside scan adds to the doctor's share — null exactly when bedsidePrice is. */
  bedsideDoctorCharge?: number | null;
  /** Named alternative rates (e.g. "LH", "MR") billed instead of the standard one. Radiology only. */
  rateOptions?: DiagnosticServiceRateOption[];
  /** What a lab test is run on (Blood, Urine…) — null for radiology or a test not placed (ADR-154). */
  sampleType?: string | null;
  /** Where it sits in the Price List — lower first; null when not placed yet (ADR-154). */
  sortOrder?: number | null;
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

/** One rate option on a Radiology service (ADR-140) — mirrors HMS.Modules.Masters.Contracts.
 * DiagnosticServiceRateOptionDto. `id` is unset for an option being added. */
export interface DiagnosticServiceRateOption {
  id?: string | null;
  name: string;
  price: number;
  /** The reporting doctor's share of this option's price. */
  doctorCharge: number;
  /** The hospital's own running cost for this option. */
  costPrice: number;
}
