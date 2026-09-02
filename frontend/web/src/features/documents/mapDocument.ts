import type { DocumentResponse, DocumentSearchQuery } from '@hms/shared';
import { API_DOCUMENT_TYPE_TO_UI, DOCUMENT_TYPE_TO_API } from './constants';
import type { DocumentFilters, HmsDocument } from './types';

export function mapDocumentResponseToHmsDocument(response: DocumentResponse): HmsDocument {
  return {
    id: response.id,
    entityType: response.ownerType,
    entityId: response.ownerId,
    documentType: API_DOCUMENT_TYPE_TO_UI[response.documentType],
    fileName: response.originalFileName,
    originalFileName: response.originalFileName,
    fileSize: response.sizeBytes,
    mimeType: response.contentType,
    uploadedBy: response.uploadedByUserId ?? '',
    uploadedAt: response.createdAt,
    isArchived: response.isArchived,
  };
}

/** Translates this UI's filter shape into the real GET /api/v1/documents query — every field
 * here is either an exact backend match (ownerType, dates, status) or a label→enum-name
 * conversion (documentType). `uploadedBy` holds a user id once resolved via a directory lookup,
 * not a display name. */
export function toDocumentSearchQuery(filters: DocumentFilters): DocumentSearchQuery {
  return {
    pageSize: 100,
    ownerType: filters.entityType,
    ownerId: filters.entityId,
    documentType: filters.documentType ? DOCUMENT_TYPE_TO_API[filters.documentType] : undefined,
    uploadedByUserId: filters.uploadedBy,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    status: filters.status,
    search: filters.search,
  };
}
