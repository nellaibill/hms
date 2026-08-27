/**
 * Minimal typed surface for the Documents module's generic upload endpoint
 * (HMS.Modules.Documents.Endpoints.DocumentsController — `POST /api/v1/documents`), used so
 * far only by Patients' photo/ID-proof upload (see documentsApi.uploadDocument and
 * usePatientMutations.ts). Deliberately not the full Documents contract surface (list/get/
 * archive/delete responses, DocumentListQuery, etc.) — the Documents Management page itself
 * is still a separate, unwired mock UI (frontend/web/src/features/documents), out of scope
 * here; this only covers what an upload call needs to send and receive.
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
  createdAt: string;
  updatedAt?: string | null;
}
