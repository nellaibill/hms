export const ENTITY_TYPES = [
  'Patient',
  'Staff',
  'Doctor',
  'Appointment',
  'Admission',
  'Lab',
  'Radiology',
  'Billing',
  'Asset',
  'Vendor',
] as const;

export type EntityType = (typeof ENTITY_TYPES)[number];

export const DOCUMENT_TYPES = [
  'ID Proof',
  'Prescription',
  'Report',
  'Invoice',
  'Consent Form',
  'Insurance',
  'Certificate',
  'Other',
] as const;

export type DocumentType = (typeof DOCUMENT_TYPES)[number];

export type DocumentStatusFilter = 'All' | 'Active' | 'Archived';

/**
 * Mirrors the single generic `Document` table (HMS.Modules.Documents) — EntityType + EntityId
 * is the only link to the owning record, so this module never needs to know anything about
 * Patients, Staff, Billing, etc. beyond that pair.
 */
export interface HmsDocument {
  id: string;
  entityType: EntityType;
  entityId: string;
  documentType: DocumentType;
  /** The backend only exposes OriginalFileName (its internal storage key is never returned) —
   * this mirrors that value; there is no separate "system" file name. */
  fileName: string;
  originalFileName: string;
  fileSize: number;
  mimeType: string;
  /** The uploader's user id (Guid) — resolve to a display name via a directory lookup
   * (see hooks/useDirectories.ts), since the API only returns the raw id. */
  uploadedBy: string;
  uploadedAt: string;
  isArchived: boolean;
}

export interface DocumentUploadFormValues {
  entityType: EntityType | '';
  entityId: string;
  documentType: DocumentType | '';
  file: File | null;
}

export interface DocumentFilters {
  entityType?: EntityType;
  entityId?: string;
  documentType?: DocumentType;
  uploadedBy?: string;
  dateFrom?: string;
  dateTo?: string;
  status: DocumentStatusFilter;
  search?: string;
}

export function createEmptyFilters(): DocumentFilters {
  return { status: 'All' };
}

export function isFiltersEmpty(filters: DocumentFilters): boolean {
  return (
    !filters.entityType &&
    !filters.entityId &&
    !filters.documentType &&
    !filters.uploadedBy &&
    !filters.dateFrom &&
    !filters.dateTo &&
    filters.status === 'All' &&
    !filters.search
  );
}
