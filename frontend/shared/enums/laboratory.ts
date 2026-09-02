/** Mirrors HMS.Modules.Laboratory.Contracts.LaboratoryEnums — serialized as strings
 * (JsonStringEnumConverter). How urgently a LabOrder's samples/results should be handled. */
export const LAB_ORDER_PRIORITIES = ['Routine', 'Urgent', 'Stat'] as const;
export type LabOrderPriority = (typeof LAB_ORDER_PRIORITIES)[number];

/** Per-item state machine — see HMS.Modules.Laboratory.Domain.LabOrderItem's mutators for the
 * exact legal transitions between these. */
export const LAB_ORDER_ITEM_STATUSES = [
  'PendingCollection',
  'Collected',
  'Received',
  'Processing',
  'ResultEntryInProgress',
  'PendingVerification',
  'CorrectionRequired',
  'Verified',
  'Rejected',
  'RecollectionRequired',
] as const;
export type LabOrderItemStatus = (typeof LAB_ORDER_ITEM_STATUSES)[number];

/** The computed, order-level status shown on the worklist — never stored, always derived from
 * every LabOrderItem's own Status plus the order's ReportGeneratedAt/ReportReleasedAt
 * timestamps. Shares most value names with LabOrderItemStatus plus two order-only states for
 * the reporting milestones. */
export const LAB_ORDER_STATUSES = [
  'PendingCollection',
  'Collected',
  'Received',
  'Processing',
  'ResultEntryInProgress',
  'PendingVerification',
  'CorrectionRequired',
  'Verified',
  'Rejected',
  'RecollectionRequired',
  'ReadyForRelease',
  'Released',
] as const;
export type LabOrderStatus = (typeof LAB_ORDER_STATUSES)[number];

/** Set/confirmed by the technician during sample collection — unset until then. */
export const LAB_SAMPLE_TYPES = ['Blood', 'Urine', 'Stool', 'Sputum', 'Swab', 'Serum', 'Plasma', 'Other'] as const;
export type LabSampleType = (typeof LAB_SAMPLE_TYPES)[number];

/** Why a collected sample was rejected — set only by RejectSample. */
export const LAB_SAMPLE_REJECTION_REASONS = [
  'InsufficientSample',
  'IncorrectSample',
  'DamagedSample',
  'HemolyzedSample',
  'WrongLabel',
  'ContaminatedSample',
  'Other',
] as const;
export type LabSampleRejectionReason = (typeof LAB_SAMPLE_REJECTION_REASONS)[number];

/** A result parameter's abnormality flag — only ever set by explicit human/API input, never
 * auto-computed: this system has no reference-range configuration data to compute it safely
 * from. */
export const LAB_RESULT_FLAGS = ['Normal', 'High', 'Low', 'Critical', 'Abnormal'] as const;
export type LabResultFlag = (typeof LAB_RESULT_FLAGS)[number];

/** One append-only audit/history entry on a LabOrderItem. Doubles as both sample-status
 * history and general audit trail. */
export const LAB_ORDER_ITEM_EVENT_TYPES = [
  'Created',
  'SampleCollected',
  'SampleRejected',
  'RecollectionRequested',
  'SampleReceived',
  'ProcessingStarted',
  'ResultDraftSaved',
  'SubmittedForVerification',
  'Verified',
  'CorrectionRequested',
] as const;
export type LabOrderItemEventType = (typeof LAB_ORDER_ITEM_EVENT_TYPES)[number];
