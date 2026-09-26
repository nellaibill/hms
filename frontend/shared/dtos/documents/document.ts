/**
 * Typed surface for the Documents module's generic endpoints
 * (HMS.Modules.Documents.Endpoints.DocumentsController). Used by Patients' and Staff's
 * photo/ID-proof upload flows (documentsApi.uploadDocument/listDocuments) as well as the
 * Document Management dashboard (frontend/web/src/features/documents), which additionally
 * uses DocumentSearchQuery/DocumentSummaryResponse below for its paged, multi-filter search
 * and KPI cards.
 */

/** Mirrors HMS.Modules.Documents.Contracts.DocumentOwnerType. */
export type DocumentOwnerType =
  | 'Patient'
  | 'Staff'
  | 'Doctor'
  | 'Appointment'
  | 'Admission'
  | 'Lab'
  | 'Radiology'
  | 'Billing'
  | 'Asset'
  | 'Vendor';

/** Mirrors HMS.Modules.Documents.Contracts.DocumentType. */
export type DocumentType = 'IdProof' | 'Prescription' | 'Report' | 'Invoice' | 'ConsentForm' | 'Insurance' | 'Certificate' | 'Other';

/** Mirrors HMS.Modules.Documents.Contracts.DocumentClassification. */
export type DocumentClassification = 'Public' | 'Internal' | 'Confidential' | 'Restricted';

/** Mirrors HMS.Modules.Documents.Contracts.DocumentStatus. */
export type DocumentStatus = 'Pending' | 'Available' | 'Quarantined';

/** Mirrors HMS.Modules.Documents.Contracts.UploadDocumentRequest — sent as multipart form
 * fields alongside the file, not JSON (see documentsApi.uploadDocument). */
export interface UploadDocumentRequest {
  ownerType: DocumentOwnerType;
  ownerId: string;
  documentType: DocumentType;
  /** Defaults to 'Internal' server-side when omitted. */
  classification?: DocumentClassification;
  /** Optional — when the document itself has a renewal/expiry date (e.g. a Staff ID proof
   * or certification). ISO date string (e.g. "2027-01-15"). */
  expiryDate?: string | null;
}

/** Mirrors HMS.Modules.Documents.Contracts.DocumentResponse. */
export interface DocumentResponse {
  id: string;
  ownerType: DocumentOwnerType;
  ownerId: string;
  documentType: DocumentType;
  classification: DocumentClassification;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  status: DocumentStatus;
  isArchived: boolean;
  uploadedByUserId?: string | null;
  /** ISO date string (e.g. "2027-01-15"), when this document has a renewal/expiry date. */
  expiryDate?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Documents.Contracts.DocumentStatusFilter. */
export type DocumentStatusFilter = 'All' | 'Active' | 'Archived';

/**
 * Query shape for GET /api/v1/documents used by the Document Management dashboard (as opposed
 * to DocumentsApi.listDocuments' narrower "one owner" shape) — mirrors
 * HMS.Modules.Documents.Contracts.DocumentListQuery, including the Page/PageSize/Sort/Search
 * fields inherited there from PagedRequest.
 */
export interface DocumentSearchQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  ownerType?: DocumentOwnerType;
  ownerId?: string;
  documentType?: DocumentType;
  uploadedByUserId?: string;
  /** ISO date string (e.g. "2027-01-15"). */
  dateFrom?: string;
  /** ISO date string (e.g. "2027-01-15"). */
  dateTo?: string;
  status?: DocumentStatusFilter;
}

/** Mirrors HMS.Modules.Documents.Contracts.DocumentSummaryResponse. */
export interface DocumentSummaryResponse {
  total: number;
  uploadedToday: number;
  archived: number;
  storageUsedBytes: number;
}
