import { API_ROUTES } from '../../constants';
import type { DocumentOwnerType, DocumentResponse, DocumentType, UploadDocumentRequest } from '../../dtos';
import type { HttpClient } from '../httpClient';

export interface DocumentListQuery {
  ownerType: DocumentOwnerType;
  ownerId: string;
  documentType?: DocumentType;
}

/**
 * Typed API service for the Documents module's generic endpoints, built on the shared HTTP
 * client (docs/FrontendArchitecture.md §6). Only covers upload/list/content — see document.ts's
 * own doc comment for why the rest of the Documents contract surface isn't modeled here yet.
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
}
