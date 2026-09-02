import type { PaginationMeta } from '@hms/shared';
import { laboratoryApi } from '@/services/apiClient';
import type {
  CollectSampleRequest,
  LabOrder,
  LabOrderListQuery,
  RejectForCorrectionRequest,
  RejectSampleRequest,
  SaveResultDraftRequest,
} from './types';

/** Real, database-backed repository (HMS.Modules.Laboratory) — thin wrapper functions calling
 * laboratoryApi.*, mirroring apiBillingRepository.ts's shape. No DTO<->app-type mapping (see
 * types.ts's own doc comment) since LabOrderResponse already matches this feature's app-level
 * shape field-for-field. */

export interface PagedLabOrders {
  items: LabOrder[];
  meta: PaginationMeta;
}

export async function listLabOrders(query: LabOrderListQuery = {}): Promise<PagedLabOrders> {
  return laboratoryApi.getOrders(query);
}

export async function getLabDashboardSummary() {
  return laboratoryApi.getDashboardSummary();
}

export async function getLabOrderById(id: string): Promise<LabOrder> {
  return laboratoryApi.getOrderById(id);
}

export async function getLabOrdersByPatientId(patientId: string): Promise<LabOrder[]> {
  return laboratoryApi.getOrdersByPatientId(patientId);
}

export async function collectSample(itemId: string, request: CollectSampleRequest): Promise<LabOrder> {
  return laboratoryApi.collectSample(itemId, request);
}

export async function rejectSample(itemId: string, request: RejectSampleRequest): Promise<LabOrder> {
  return laboratoryApi.rejectSample(itemId, request);
}

export async function requestRecollection(itemId: string): Promise<LabOrder> {
  return laboratoryApi.requestRecollection(itemId);
}

export async function receiveSample(itemId: string): Promise<LabOrder> {
  return laboratoryApi.receiveSample(itemId);
}

export async function startProcessing(itemId: string): Promise<LabOrder> {
  return laboratoryApi.startProcessing(itemId);
}

export async function saveResultDraft(itemId: string, request: SaveResultDraftRequest): Promise<LabOrder> {
  return laboratoryApi.saveResultDraft(itemId, request);
}

export async function submitForVerification(itemId: string): Promise<LabOrder> {
  return laboratoryApi.submitForVerification(itemId);
}

export async function verifyResult(itemId: string): Promise<LabOrder> {
  return laboratoryApi.verify(itemId);
}

export async function rejectForCorrection(itemId: string, request: RejectForCorrectionRequest): Promise<LabOrder> {
  return laboratoryApi.rejectForCorrection(itemId, request);
}

export async function generateReport(orderId: string): Promise<LabOrder> {
  return laboratoryApi.generateReport(orderId);
}

export async function releaseReport(orderId: string): Promise<LabOrder> {
  return laboratoryApi.releaseReport(orderId);
}
