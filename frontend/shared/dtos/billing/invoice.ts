import type { BillingType, Gender, InvoicePaymentStatus, PaymentMethod, VisitType } from '../../enums';

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceLineItemRequest. */
export interface CreateInvoiceLineItemRequest {
  billingType: BillingType;
  departmentId?: string | null;
  consultantId?: string | null;
  serviceId?: string | null;
  /** App-level reference into Masters' DiagnosticPackage — set for a package line, null for a
   * plain service line. */
  packageId?: string | null;
  quantity: number;
  unitPrice: number;
  /** The discount asked for on this line (ADR-109). The server decides whether it applies: a
   * Super Admin's applies straight away, anyone else's becomes a discount request and the line
   * stays at full price until a Super Admin approves it. */
  discount: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoicePaymentRequest — one payment-method row. */
export interface CreateInvoicePaymentRequest {
  method: PaymentMethod;
  /** How much was tendered via this method. */
  amount: number;
  referenceNumber?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceRequest. */
export interface CreateInvoiceRequest {
  patientId: string;
  visitId: string;
  patientName: string;
  patientUhid: string;
  items: CreateInvoiceLineItemRequest[];
  /** Optional — when supplied, the whole invoice is paid in full at creation time. Null/undefined/
   * empty saves Pending, exactly like before this field existed. One row pays in full with a
   * single method (may tender more than the net total, with the excess treated as change and
   * not stored); more than one row is a split payment across methods and must add up to exactly
   * the net total. See Contracts/InvoiceContracts.cs. */
  payments?: CreateInvoicePaymentRequest[] | null;
  /** Required, with `discountValue` and `discountReason`, when any line has a discount (ADR-109).
   * Display only — the per-line discounts are what is applied. A non-Super Admin's discounted
   * bill must come without `payments`: it is saved unpaid until the discount is decided. */
  discountType?: InvoiceDiscountType;
  discountValue?: number;
  discountReason?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.PayInvoiceRequest — collects everything still owed on a
 * bill. Empty only when nothing is owed (a fully discounted bill). */
export interface PayInvoiceRequest {
  payments: CreateInvoicePaymentRequest[];
}

/** Mirrors HMS.Modules.Billing.Contracts.RecordPaymentRequest. */
export interface RecordPaymentRequest {
  method: PaymentMethod;
}

/** Mirrors HMS.Modules.Billing.Contracts.VoidInvoiceRequest. */
export interface VoidInvoiceRequest {
  reason: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceLineItemResponse. */
export interface InvoiceLineItemResponse {
  id: string;
  billingType: BillingType;
  departmentId?: string | null;
  consultantId?: string | null;
  /** Same value as `consultantId` at creation, but survives payment (unlike `consultantId`,
   * which the backend clears once paid) — for per-consultant revenue reporting. */
  billedConsultantId?: string | null;
  serviceId?: string | null;
  packageId?: string | null;
  quantity: number;
  unitPrice: number;
  discount: number;
  discountApproved: boolean;
  discountApprovedBy?: string | null;
  paymentStatus: InvoicePaymentStatus;
  total: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceResponse. */
export interface InvoiceResponse {
  id: string;
  invoiceNumber: string;
  patientId: string;
  visitId: string;
  patientName: string;
  patientUhid: string;
  createdAt: string;
  items: InvoiceLineItemResponse[];
  grossAmount: number;
  totalDiscount: number;
  netAmount: number;
  paymentStatus: InvoicePaymentStatus;
  isVoided: boolean;
  voidedAt?: string | null;
  voidReason?: string | null;
  /** True when the void came from an approved OP bill revert request (ADR-106). */
  isReverted: boolean;
  /** The state of the bill's latest discount request (ADR-109); null when none was ever raised.
   * While Pending the bill can't be paid. */
  discountStatus?: InvoiceDiscountRequestStatus | null;
  /** The visit's OP No — set only on the create/record-payment response once the visit's
   * consultation fee is paid, which is when an OP visit gets its number (ADR-103). */
  opNumber?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceListQuery. */
export interface InvoiceListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  paymentStatus?: InvoicePaymentStatus;
}

/** Mirrors HMS.Modules.Billing.Contracts.RecentBillConsultationResponse. */
export interface RecentBillConsultation {
  departmentId: string;
  consultantId: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.RecentPatientBillResponse — one row of the Patient
 * Billing page's "Recent Patient Bills" table. Age/gender/contact/registrationType/consultants
 * come back null/empty (never an error) when the source patient or visit record can't be
 * resolved — see the backend contract's own doc comment. */
export interface RecentPatientBill {
  invoiceId: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  age?: number | null;
  gender?: Gender | null;
  contactNumber?: string | null;
  registrationType?: VisitType | null;
  consultants: RecentBillConsultation[];
  appointmentDateTime: string;
  netAmount: number;
  paymentStatus: InvoicePaymentStatus;
  isVoided: boolean;
  isReverted: boolean;
  discountStatus?: InvoiceDiscountRequestStatus | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.ProcedureListItem — one row of the OPD Procedures
 * List tab. `serviceId`/`consultantId`/`departmentId` are typed as free-text strings (matching
 * InvoiceLineItem's own storage — not Guids/lookup ids) but hold Masters Guids in practice
 * (ServiceBillingCard.tsx's Procedure Billing form picks a real DiagnosticService/Consultant).
 * The ids are returned as-is; `serviceName`/`consultantName`/`departmentName` are resolved
 * alongside them by the backend for display — same id-plus-name shape as every other list item
 * in this codebase (e.g. OpdPatientListItem's departmentId/departmentName), so use the *Name
 * fields to render and the plain ids for anything that needs the real id (filtering, linking). */
export interface ProcedureListItem {
  invoiceId: string;
  invoiceLineItemId: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  serviceId?: string | null;
  serviceName?: string | null;
  consultantId?: string | null;
  consultantName?: string | null;
  /** Only set when this procedure had a department — Procedure Billing doesn't currently
   * collect one. */
  departmentId?: string | null;
  departmentName?: string | null;
  createdAt: string;
  paymentStatus: InvoicePaymentStatus;
  total: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.ProcedureListQuery. `departmentId`/`consultantId`
 * filter against the same free-text strings ProcedureListItem carries, not Guids. */
export interface ProcedureListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  from?: string;
  to?: string;
  departmentId?: string;
  consultantId?: string;
  paymentStatus?: InvoicePaymentStatus;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceRevertRequestStatus. */
export type InvoiceRevertRequestStatus = 'Pending' | 'Approved' | 'Rejected';

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceRevertRequest — the reason is required. */
export interface CreateInvoiceRevertRequest {
  reason: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.ApproveInvoiceRevertRequest. */
export interface ApproveInvoiceRevertRequest {
  notes?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.RejectInvoiceRevertRequest — notes are required. */
export interface RejectInvoiceRevertRequest {
  notes: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceRevertRequestResponse — an OP bill revert
 * request (ADR-106). Invoice number, patient and amounts are snapshotted when it was raised. */
export interface InvoiceRevertRequestResponse {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  netAmount: number;
  /** What had been collected when the request was raised — the amount to refund on approval. */
  paidAmount: number;
  reason: string;
  status: InvoiceRevertRequestStatus;
  requestedBy?: string | null;
  requestedByName?: string | null;
  requestedAt: string;
  decidedBy?: string | null;
  decidedByName?: string | null;
  decidedAt?: string | null;
  decisionNotes?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceRevertStatusResponse — one invoice's revert state. */
export interface InvoiceRevertStatusResponse {
  canRequest: boolean;
  ineligibleReason?: string | null;
  /** Newest first. */
  requests: InvoiceRevertRequestResponse[];
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceRevertRequestListQuery. */
export interface InvoiceRevertRequestListQuery {
  page?: number;
  pageSize?: number;
  status?: InvoiceRevertRequestStatus;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountRequestStatus — also an invoice's own
 * `discountStatus` (its latest request). Cancelled when the bill was voided while waiting. */
export type InvoiceDiscountRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountType — how the counter entered it. */
export type InvoiceDiscountType = 'Amount' | 'Percent';

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountLineRequest. */
export interface InvoiceDiscountLineRequest {
  lineItemId: string;
  discount: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.CreateInvoiceDiscountRequest — a discount on lines of an
 * existing unpaid bill (ADR-109). The reason is required. */
export interface CreateInvoiceDiscountRequest {
  discountType: InvoiceDiscountType;
  discountValue: number;
  reason: string;
  lines: InvoiceDiscountLineRequest[];
}

/** Mirrors HMS.Modules.Billing.Contracts.ApproveInvoiceDiscountRequest. */
export interface ApproveInvoiceDiscountRequest {
  notes?: string | null;
}

/** Mirrors HMS.Modules.Billing.Contracts.RejectInvoiceDiscountRequest — notes are required. */
export interface RejectInvoiceDiscountRequest {
  notes: string;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountRequestLineResponse — one discounted line,
 * snapshotted from the bill line when the request was raised. */
export interface InvoiceDiscountRequestLineResponse {
  lineItemId: string;
  billingType: BillingType;
  departmentId?: string | null;
  consultantId?: string | null;
  serviceId?: string | null;
  packageId?: string | null;
  quantity: number;
  unitPrice: number;
  /** quantity × unitPrice — the line's charge before this discount. */
  lineAmount: number;
  discount: number;
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountRequestResponse (ADR-109). */
export interface InvoiceDiscountRequestResponse {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  /** The whole bill's gross amount when the request was raised. */
  grossAmount: number;
  discountType: InvoiceDiscountType;
  discountValue: number;
  totalDiscount: number;
  reason: string;
  status: InvoiceDiscountRequestStatus;
  requestedBy?: string | null;
  requestedByName?: string | null;
  requestedAt: string;
  decidedBy?: string | null;
  decidedByName?: string | null;
  decidedAt?: string | null;
  decisionNotes?: string | null;
  lines: InvoiceDiscountRequestLineResponse[];
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountStatusResponse — one invoice's discount state. */
export interface InvoiceDiscountStatusResponse {
  canRequest: boolean;
  ineligibleReason?: string | null;
  /** Newest first. */
  requests: InvoiceDiscountRequestResponse[];
}

/** Mirrors HMS.Modules.Billing.Contracts.InvoiceDiscountRequestListQuery. */
export interface InvoiceDiscountRequestListQuery {
  page?: number;
  pageSize?: number;
  status?: InvoiceDiscountRequestStatus;
}
