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
  discount: number;
  discountApproved: boolean;
  discountApprovedBy?: string | null;
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
