import { API_ROUTES } from '../../constants';
import type { DocumentOwnerType, DocumentResponse, DocumentSearchQuery, DocumentSummaryResponse, DocumentType, UploadDocumentRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface DocumentListQuery {
  ownerType: DocumentOwnerType;
  ownerId: string;
  documentType?: DocumentType;
}

export interface PagedDocuments {
  items: DocumentResponse[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Documents module's generic endpoints, built on the shared HTTP
 * client (docs/FrontendArchitecture.md §6).
 */
export class DocumentsApi {
  constructor(private readonly client: HttpClient) {}

  async uploadDocument(file: File, request: UploadDocumentRequest): Promise<DocumentResponse> {
    const formData = new FormData();
    // Field name must be "file" — mirrors DocumentsController.Upload's IFormFile parameter
    // name, which ASP.NET Core model binding matches against.
    formData.append('file', file);
    formData.append('ownerType', request.ownerType);
    formData.append('ownerId', request.ownerId);
    formData.append('documentType', request.documentType);
    if (request.classification) {
      formData.append('classification', request.classification);
    }
    if (request.expiryDate) {
      formData.append('expiryDate', request.expiryDate);
    }
    const response = await this.client.postFormData<DocumentResponse>(API_ROUTES.documents.base, formData);
    return response.data;
  }

  /** Lists documents for one owner, optionally narrowed to one document type — e.g. a
   * patient's uploaded photo/ID-proof (see usePatientDocumentUrl). Not paged here; every
   * caller so far only needs "the handful of documents for this one owner+type." */
  async listDocuments(query: DocumentListQuery): Promise<DocumentResponse[]> {
    const response = await this.client.get<DocumentResponse[]>(API_ROUTES.documents.base, {
      query: { ownerType: query.ownerType, ownerId: query.ownerId, documentType: query.documentType },
    });
    return response.data;
  }

  async getDocumentContent(id: string): Promise<Blob> {
    return this.client.getBlob(API_ROUTES.documents.content(id));
  }

  async deleteDocument(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.documents.byId(id));
  }

  /** Paged, multi-filter search across every owner — backs the Document Management dashboard
   * (frontend/web/src/features/documents), as opposed to listDocuments' "one owner" shape used
   * by the Patient/Employee document tabs. Mirrors DocumentsController.GetPaged. */
  async getDocuments(query: DocumentSearchQuery = {}): Promise<PagedDocuments> {
    const response = await this.client.get<DocumentResponse[]>(API_ROUTES.documents.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        ownerType: query.ownerType,
        ownerId: query.ownerId,
        documentType: query.documentType,
        uploadedByUserId: query.uploadedByUserId,
        dateFrom: query.dateFrom,
        dateTo: query.dateTo,
        status: query.status,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  /** Server-computed KPI aggregate — mirrors DocumentsController.GetSummary. */
  async getSummary(): Promise<DocumentSummaryResponse> {
    const response = await this.client.get<DocumentSummaryResponse>(API_ROUTES.documents.summary);
    return response.data;
  }

  /** Mirrors DocumentsController.Archive (idempotent). */
  async archiveDocument(id: string): Promise<DocumentResponse> {
    const response = await this.client.patch<DocumentResponse>(API_ROUTES.documents.archive(id));
    return response.data;
  }
}
