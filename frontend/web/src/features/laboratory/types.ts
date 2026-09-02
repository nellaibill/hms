/**
 * Laboratory's app-level type set — unlike features/billing/types.ts, this is a thin re-export
 * of the shared DTOs (renamed to drop the "Response"/"Request" suffix for in-app readability),
 * not a separate mapped shape. LabOrderResponse/LabOrderItemResponse already match how this
 * feature's components want to consume them field-for-field, so a DTO<->app-type mapping layer
 * in apiLaboratoryRepository.ts would only add boilerplate with no real translation happening —
 * see the module rollout spec's own note that this is a judgment call per-feature.
 */
export type {
  CollectSampleRequest,
  LabDashboardSummaryResponse as LabDashboardSummary,
  LabOrderItemEventResponse as LabOrderItemEvent,
  LabOrderItemResponse as LabOrderItem,
  LabOrderListQuery,
  LabOrderResponse as LabOrder,
  RejectForCorrectionRequest,
  RejectSampleRequest,
  ResultParameterRequest,
  ResultParameterResponse as LabResultParameter,
  SaveResultDraftRequest,
} from '@hms/shared';

export type {
  LabOrderItemEventType,
  LabOrderItemStatus,
  LabOrderPriority,
  LabOrderStatus,
  LabResultFlag,
  LabSampleRejectionReason,
  LabSampleType,
} from '@hms/shared';

export {
  LAB_ORDER_ITEM_EVENT_TYPES,
  LAB_ORDER_ITEM_STATUSES,
  LAB_ORDER_PRIORITIES,
  LAB_ORDER_STATUSES,
  LAB_RESULT_FLAGS,
  LAB_SAMPLE_REJECTION_REASONS,
  LAB_SAMPLE_TYPES,
} from '@hms/shared';
