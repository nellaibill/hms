import { ALLOWED_MIME_TYPES, MAX_FILE_SIZE_BYTES, VALIDATION_MESSAGES } from './constants';
import { entityExists } from './mockEntities';
import type { DocumentUploadFormValues } from './types';

export interface UploadFormErrors {
  entityType?: string;
  entity?: string;
  documentType?: string;
  file?: string;
}

export function validateUploadForm(values: DocumentUploadFormValues): UploadFormErrors {
  const errors: UploadFormErrors = {};

  if (!values.entityType) {
    errors.entityType = VALIDATION_MESSAGES.entityTypeRequired;
  } else if (!values.entityId || !entityExists(values.entityType, values.entityId)) {
    errors.entity = VALIDATION_MESSAGES.entityRequired;
  }

  if (!values.documentType) {
    errors.documentType = VALIDATION_MESSAGES.documentTypeRequired;
  }

  if (!values.file) {
    errors.file = VALIDATION_MESSAGES.fileRequired;
  } else if (!ALLOWED_MIME_TYPES.includes(values.file.type)) {
    errors.file = VALIDATION_MESSAGES.unsupportedFormat;
  } else if (values.file.size > MAX_FILE_SIZE_BYTES) {
    errors.file = VALIDATION_MESSAGES.fileTooLarge;
  }

  return errors;
}

export function isUploadFormValid(errors: UploadFormErrors): boolean {
  return Object.keys(errors).length === 0;
}
