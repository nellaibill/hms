/** Shared between DocumentUploadStaging (create wizard) and PatientDocumentUpload (edit
 * screen) so the two can't quietly drift to different caps. See fileValidation.ts's own doc
 * comment for why this is a client-side convenience check, not the real security boundary. */
export const MAX_PATIENT_DOCUMENT_SIZE_MB = 2;
export const MAX_PATIENT_DOCUMENT_SIZE_BYTES = MAX_PATIENT_DOCUMENT_SIZE_MB * 1024 * 1024;
