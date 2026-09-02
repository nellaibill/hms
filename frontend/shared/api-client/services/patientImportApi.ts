import { API_ROUTES } from '../../constants';
import type { ImportBatch, ImportBatchListQuery, ImportRow, ImportRowListQuery } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedImportBatches {
  items: ImportBatch[];
  meta: PaginationMeta;
}

export interface PagedImportRows {
  items: ImportRow[];
  meta: PaginationMeta;
}

/**
 * Typed API service for bulk patient import (Super Admin only), built on the shared HTTP
 * client — mirrors HMS.Modules.Patients.Endpoints.PatientImportController.
 */
export class PatientImportApi {
  constructor(private readonly client: HttpClient) {}

  /** Downloads the blank .xlsx template. */
  async getTemplate(): Promise<Blob> {
    return this.client.getBlob(API_ROUTES.patientImport.template);
  }

  /** Uploads a filled-in template. Queues the validate pass — nothing is written to
   * patients/addresses by this call. */
  async upload(file: File): Promise<ImportBatch> {
    const formData = new FormData();
    // Field name must be "file" — mirrors PatientImportController.Upload's IFormFile
    // parameter name, which ASP.NET Core model binding matches against.
    formData.append('file', file);
    const response = await this.client.postFormData<ImportBatch>(API_ROUTES.patientImport.base, formData);
    return response.data;
  }

  async getBatch(batchId: string): Promise<ImportBatch> {
    const response = await this.client.get<ImportBatch>(API_ROUTES.patientImport.byId(batchId));
    return response.data;
  }

  /** Import History — every past and in-progress batch, newest first. */
  async getBatches(query: ImportBatchListQuery = {}): Promise<PagedImportBatches> {
    const response = await this.client.get<ImportBatch[]>(API_ROUTES.patientImport.base, {
      query: { page: query.page, pageSize: query.pageSize },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  /** Paginated row detail for the review screen — pass status: 'Invalid' to show only what
   * was skipped. */
  async getRows(batchId: string, query: ImportRowListQuery = {}): Promise<PagedImportRows> {
    const response = await this.client.get<ImportRow[]>(API_ROUTES.patientImport.rows(batchId), {
      query: { status: query.status, page: query.page, pageSize: query.pageSize },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  /** Downloads every skipped row with the reason(s) it was skipped. */
  async getReport(batchId: string): Promise<Blob> {
    return this.client.getBlob(API_ROUTES.patientImport.report(batchId));
  }

  /** Confirms the import — only valid once the batch is ReadyForReview. Writes nothing
   * itself; queues the pass that actually creates the patients. */
  async commit(batchId: string): Promise<ImportBatch> {
    const response = await this.client.post<ImportBatch>(API_ROUTES.patientImport.commit(batchId));
    return response.data;
  }
}
