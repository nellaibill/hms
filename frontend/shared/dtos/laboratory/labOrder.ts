import type {
  LabOrderItemEventType,
  LabOrderItemStatus,
  LabOrderPriority,
  LabOrderStatus,
  LabResultFlag,
  LabSampleRejectionReason,
  LabSampleType,
} from '../../enums';

/** Mirrors HMS.Modules.Laboratory.Contracts.CollectSampleRequest. */
export interface CollectSampleRequest {
  sampleType: LabSampleType;
  location?: string | null;
  quantity?: string | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.RejectSampleRequest. */
export interface RejectSampleRequest {
  reason: LabSampleRejectionReason;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.ResultParameterRequest. */
export interface ResultParameterRequest {
  parameterName: string;
  resultValue: string;
  unit?: string | null;
  referenceRange?: string | null;
  flag?: LabResultFlag | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.SaveResultDraftRequest — replaces the item's full
 * parameter set (not an incremental add/edit/remove). */
export interface SaveResultDraftRequest {
  parameters: ResultParameterRequest[];
}

/** Mirrors HMS.Modules.Laboratory.Contracts.RejectForCorrectionRequest. */
export interface RejectForCorrectionRequest {
  reason: string;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.LabOrderListQuery — standard paging/sort/search
 * shape plus the worklist's own status/priority/date-range filters. */
export interface LabOrderListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  status?: LabOrderStatus;
  priority?: LabOrderPriority;
  dateFrom?: string;
  dateTo?: string;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.ResultParameterResponse. */
export interface ResultParameterResponse {
  id: string;
  parameterName: string;
  resultValue: string;
  unit?: string | null;
  referenceRange?: string | null;
  flag?: LabResultFlag | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.LabOrderItemEventResponse. */
export interface LabOrderItemEventResponse {
  id: string;
  eventType: LabOrderItemEventType;
  actorId?: string | null;
  occurredAt: string;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.Laboratory.Contracts.LabOrderItemResponse. */
export interface LabOrderItemResponse {
  id: string;
  serviceId: string;
  packageId?: string | null;
  testName: string;
  departmentId?: string | null;
  consultantId?: string | null;
  sampleType?: LabSampleType | null;
  status: LabOrderItemStatus;

  collectedAt?: string | null;
  collectedBy?: string | null;
  collectionLocation?: string | null;
  sampleQuantity?: string | null;
  collectionRemarks?: string | null;

  rejectionReason?: LabSampleRejectionReason | null;
  rejectionRemarks?: string | null;
  rejectedAt?: string | null;
  rejectedBy?: string | null;

  submittedForVerificationAt?: string | null;
  verifiedAt?: string | null;
  verifiedBy?: string | null;
  correctionReason?: string | null;
  correctionRequestedAt?: string | null;

  parameters: ResultParameterResponse[];
  events: LabOrderItemEventResponse[];
}

/** Mirrors HMS.Modules.Laboratory.Contracts.LabOrderResponse. */
export interface LabOrderResponse {
  id: string;
  labOrderNumber: string;
  invoiceId: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  visitId: string;
  source?: string | null;
  priority: LabOrderPriority;
  overallStatus: LabOrderStatus;
  createdAt: string;
  reportGeneratedAt?: string | null;
  reportGeneratedBy?: string | null;
  reportReleasedAt?: string | null;
  reportReleasedBy?: string | null;
  items: LabOrderItemResponse[];
}

/** Mirrors HMS.Modules.Laboratory.Contracts.LabDashboardSummaryResponse — backs the lab
 * worklist dashboard's summary tiles. "Today" = CreatedAt's date matches server UTC today. */
export interface LabDashboardSummaryResponse {
  totalRequestsToday: number;
  pendingSampleCollection: number;
  samplesCollected: number;
  samplesReceived: number;
  testsInProgress: number;
  resultsPendingEntry: number;
  pendingVerification: number;
  reportsReady: number;
  reportsReleased: number;
  rejectedOrRecollectionRequired: number;
}
