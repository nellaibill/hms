import type { ImportBatchStatus, ImportRowStatus } from '../../enums/patients';

/** Mirrors HMS.Modules.Patients.Contracts.ImportBatchResponse. */
export interface ImportBatch {
  id: string;
  fileName: string;
  status: ImportBatchStatus;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  createdRows: number;
  commitFailedRows: number;
  uploadedAt: string;
  uploadedBy?: string | null;
  committedAt?: string | null;
  committedBy?: string | null;
}

/** Mirrors HMS.Modules.Patients.Contracts.ImportRowError. */
export interface ImportRowError {
  field: string;
  message: string;
}

/** Mirrors HMS.Modules.Patients.Contracts.ImportRowResponse. */
export interface ImportRow {
  id: string;
  rowNumber: number;
  status: ImportRowStatus;
  rawData: Record<string, string | null>;
  errors: ImportRowError[];
  createdPatientId?: string | null;
}

export interface ImportBatchListQuery {
  page?: number;
  pageSize?: number;
}

export interface ImportRowListQuery {
  status?: ImportRowStatus;
  page?: number;
  pageSize?: number;
}
