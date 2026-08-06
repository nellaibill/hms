import { buildMockDocuments } from './mockDocuments';
import { isUploadFormValid, validateUploadForm } from './validation';
import { todayIsoDate } from './utils/format';
import type { DocumentFilters, DocumentUploadFormValues, HmsDocument } from './types';

/**
 * UI-only mock data layer — Document Management has no backend module yet (mirrors
 * features/calendarEvents/mockEventsStore.ts). Persisted to localStorage so the demo survives
 * page refreshes, except blob: object URLs (created for real uploaded files via
 * URL.createObjectURL) — those die with the page, so they're swapped for a sentinel before
 * persisting and gracefully fall back to the "no preview available" state after a reload.
 */
const STORAGE_KEY = 'hms-mock-documents';
const EXPIRED_CONTENT_MARKER = 'session://expired';

function hasLiveContent(filePath: string): boolean {
  return filePath.startsWith('blob:') || filePath.startsWith('data:');
}

function sanitizeForStorage(doc: HmsDocument): HmsDocument {
  return doc.filePath.startsWith('blob:') ? { ...doc, filePath: EXPIRED_CONTENT_MARKER } : doc;
}

function loadDocuments(): HmsDocument[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as HmsDocument[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return buildMockDocuments();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(documents.map(sanitizeForStorage)));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let documents: HmsDocument[] = loadDocuments();
let nextSeq = documents.reduce((max, d) => Math.max(max, Number(d.id.replace('doc-', '')) || 0), 0) + 1;

export type DocumentListQuery = DocumentFilters;

function withinDateRange(uploadedAt: string, from?: string, to?: string): boolean {
  const day = uploadedAt.slice(0, 10);
  if (from && day < from) return false;
  if (to && day > to) return false;
  return true;
}

export function listMockDocuments(query: DocumentListQuery): HmsDocument[] {
  let items = documents;

  if (query.entityType) items = items.filter((d) => d.entityType === query.entityType);
  if (query.entityId) items = items.filter((d) => d.entityId === query.entityId);
  if (query.documentType) items = items.filter((d) => d.documentType === query.documentType);
  if (query.uploadedBy) items = items.filter((d) => d.uploadedBy === query.uploadedBy);
  if (query.dateFrom || query.dateTo) items = items.filter((d) => withinDateRange(d.uploadedAt, query.dateFrom, query.dateTo));
  if (query.status === 'Active') items = items.filter((d) => !d.isArchived);
  if (query.status === 'Archived') items = items.filter((d) => d.isArchived);

  const search = query.search?.trim().toLowerCase();
  if (search) {
    items = items.filter((d) => [d.fileName, d.originalFileName].some((field) => field.toLowerCase().includes(search)));
  }

  return [...items].sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
}

export function getMockDocumentById(id: string): HmsDocument | undefined {
  return documents.find((d) => d.id === id);
}

export interface DocumentSummaryStats {
  total: number;
  uploadedToday: number;
  archived: number;
  storageUsedBytes: number;
}

export function getMockDocumentSummary(): DocumentSummaryStats {
  const today = todayIsoDate();
  return {
    total: documents.length,
    uploadedToday: documents.filter((d) => d.uploadedAt.slice(0, 10) === today).length,
    archived: documents.filter((d) => d.isArchived).length,
    storageUsedBytes: documents.reduce((sum, d) => sum + d.fileSize, 0),
  };
}

export function getMockUploaders(): string[] {
  return [...new Set(documents.map((d) => d.uploadedBy))].sort();
}

function slugify(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9.]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

export class DocumentValidationError extends Error {}

export function createMockDocument(values: DocumentUploadFormValues): HmsDocument {
  const errors = validateUploadForm(values);
  if (!isUploadFormValid(errors)) {
    throw new DocumentValidationError(Object.values(errors)[0] ?? 'Invalid upload.');
  }

  const file = values.file as File;
  const seq = nextSeq++;
  const id = `doc-${String(seq).padStart(3, '0')}`;
  const doc: HmsDocument = {
    id,
    entityType: values.entityType as HmsDocument['entityType'],
    entityId: values.entityId,
    documentType: values.documentType as HmsDocument['documentType'],
    fileName: `${id}-${slugify(file.name)}`,
    originalFileName: file.name,
    filePath: URL.createObjectURL(file),
    fileSize: file.size,
    mimeType: file.type,
    uploadedBy: 'Admin User',
    uploadedAt: new Date().toISOString(),
    isArchived: false,
  };
  documents = [doc, ...documents];
  persist();
  return doc;
}

export function archiveMockDocument(id: string): HmsDocument {
  const existing = getMockDocumentById(id);
  if (!existing) throw new Error(`Mock document ${id} not found.`);
  const updated: HmsDocument = { ...existing, isArchived: true };
  documents = documents.map((d) => (d.id === id ? updated : d));
  persist();
  return updated;
}

export function deleteMockDocument(id: string): void {
  const existing = getMockDocumentById(id);
  if (existing && existing.filePath.startsWith('blob:')) {
    URL.revokeObjectURL(existing.filePath);
  }
  documents = documents.filter((d) => d.id !== id);
  persist();
}

export { hasLiveContent };
