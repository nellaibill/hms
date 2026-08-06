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
 * Mirrors the single generic `Document` table — EntityType + EntityId is the only link to the
 * owning record, so this module never needs to know anything about Patients, Staff, Billing,
 * etc. beyond that pair.
 */
export interface HmsDocument {
  id: string;
  entityType: EntityType;
  entityId: string;
  documentType: DocumentType;
  fileName: string;
  originalFileName: string;
  /** Object URL (session-only) for real uploads, or a small embedded demo asset for seed data — never a server path, since there's no backend yet. */
  filePath: string;
  fileSize: number;
  mimeType: string;
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
