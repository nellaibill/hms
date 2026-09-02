import type { DocumentType as ApiDocumentType } from '@hms/shared';
import type { DocumentType, EntityType } from './types';

export interface EntityTypeMeta {
  label: string;
  chipClass: string;
}

// Categorical palette for the Entity Type badge column — distinct from the semantic
// success/warning/error tokens (§4 of the design system) since these are pure category
// labels, not status. Kept in one place so every module's documents render identically.
export const ENTITY_TYPE_META: Record<EntityType, EntityTypeMeta> = {
  Patient: { label: 'Patient', chipClass: 'bg-sky-50 text-sky-700 border-sky-200 dark:bg-sky-500/10 dark:text-sky-300 dark:border-sky-500/30' },
  Staff: { label: 'Staff', chipClass: 'bg-teal-50 text-teal-700 border-teal-200 dark:bg-teal-500/10 dark:text-teal-300 dark:border-teal-500/30' },
  Doctor: { label: 'Doctor', chipClass: 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-500/10 dark:text-indigo-300 dark:border-indigo-500/30' },
  Appointment: { label: 'Appointment', chipClass: 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-300 dark:border-amber-500/30' },
  Admission: { label: 'Admission', chipClass: 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-500/10 dark:text-rose-300 dark:border-rose-500/30' },
  Lab: { label: 'Laboratory', chipClass: 'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-500/10 dark:text-violet-300 dark:border-violet-500/30' },
  Radiology: { label: 'Radiology', chipClass: 'bg-fuchsia-50 text-fuchsia-700 border-fuchsia-200 dark:bg-fuchsia-500/10 dark:text-fuchsia-300 dark:border-fuchsia-500/30' },
  Billing: { label: 'Billing', chipClass: 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-300 dark:border-emerald-500/30' },
  Asset: { label: 'Asset', chipClass: 'bg-slate-100 text-slate-700 border-slate-300 dark:bg-slate-500/10 dark:text-slate-300 dark:border-slate-500/30' },
  Vendor: { label: 'Vendor', chipClass: 'bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-500/10 dark:text-orange-300 dark:border-orange-500/30' },
};

export const DOCUMENT_TYPE_LABELS: Record<DocumentType, string> = {
  'ID Proof': 'ID Proof',
  Prescription: 'Prescription',
  Report: 'Report',
  Invoice: 'Invoice',
  'Consent Form': 'Consent Form',
  Insurance: 'Insurance',
  Certificate: 'Certificate',
  Other: 'Other',
};

// Translates between this UI's display labels and HMS.Modules.Documents.Contracts.DocumentType's
// enum member names, which the backend model binder matches by name (e.g. query/form string
// "IdProof", not "ID Proof").
export const DOCUMENT_TYPE_TO_API: Record<DocumentType, ApiDocumentType> = {
  'ID Proof': 'IdProof',
  Prescription: 'Prescription',
  Report: 'Report',
  Invoice: 'Invoice',
  'Consent Form': 'ConsentForm',
  Insurance: 'Insurance',
  Certificate: 'Certificate',
  Other: 'Other',
};

export const API_DOCUMENT_TYPE_TO_UI: Record<ApiDocumentType, DocumentType> = {
  IdProof: 'ID Proof',
  Prescription: 'Prescription',
  Report: 'Report',
  Invoice: 'Invoice',
  ConsentForm: 'Consent Form',
  Insurance: 'Insurance',
  Certificate: 'Certificate',
  Other: 'Other',
};

export interface AllowedFileType {
  mimeType: string;
  extensionLabel: string;
}

// Configurable — a real deployment would source this (and MAX_FILE_SIZE_MB below) from
// server-side app config; hardcoded here since there's no backend yet.
export const ALLOWED_FILE_TYPES: AllowedFileType[] = [
  { mimeType: 'application/pdf', extensionLabel: 'PDF' },
  { mimeType: 'image/jpeg', extensionLabel: 'JPG' },
  { mimeType: 'image/png', extensionLabel: 'PNG' },
  { mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', extensionLabel: 'DOCX' },
  { mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', extensionLabel: 'XLSX' },
];

export const ALLOWED_MIME_TYPES = ALLOWED_FILE_TYPES.map((t) => t.mimeType);
export const ACCEPTED_EXTENSIONS_LABEL = ALLOWED_FILE_TYPES.map((t) => t.extensionLabel).join(', ');
export const ACCEPT_ATTRIBUTE = [...ALLOWED_MIME_TYPES, '.jpg', '.jpeg', '.png', '.pdf', '.docx', '.xlsx'].join(',');

/** Configurable maximum upload size — see note on ALLOWED_FILE_TYPES above. */
export const MAX_FILE_SIZE_MB = 10;
export const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

export const VALIDATION_MESSAGES = {
  fileRequired: 'Please select a file.',
  unsupportedFormat: 'Unsupported file format.',
  fileTooLarge: 'File exceeds maximum allowed size.',
  entityRequired: 'Please select an entity.',
  entityTypeRequired: 'Please select an entity type.',
  documentTypeRequired: 'Please select a document type.',
} as const;
