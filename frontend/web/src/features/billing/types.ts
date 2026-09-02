export const BILLING_TYPES = ['Consultation', 'Radiology', 'Laboratory', 'Procedure', 'Injection', 'File'] as const;
export type BillingType = (typeof BILLING_TYPES)[number];

export const PAYMENT_STATUSES = ['Pending', 'Paid'] as const;
export type PaymentStatus = (typeof PAYMENT_STATUSES)[number];

/** Mirrors HMS.Modules.Billing.Contracts.PaymentMethod. */
export const PAYMENT_METHODS = ['Cash', 'Card', 'Upi', 'BankTransfer'] as const;
export type PaymentMethod = (typeof PAYMENT_METHODS)[number];
export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  Cash: 'Cash',
  Card: 'Card',
  Upi: 'UPI',
  BankTransfer: 'Bank Transfer',
};

/**
 * One priced line on a bill. Normalized so future billing types (packages, insurance, GST
 * line items, refunds…) are just another `billingType` value on the same shape, not a new
 * one — and so a category can later hold many items (multiple lab tests, multiple
 * procedures) without changing this type at all.
 */
export interface BillingItem {
  id: string;
  /** Wider than this file's own BillingType/BILLING_TYPES (which drives which manual-entry
   * cards the registration/OPD Billing Entry wizard renders — Consultation/Radiology/
   * Laboratory/Procedure only): a real invoice's line item can also be 'Pharmacy', generated
   * server-side only by DispenseService's best-effort billing step (ADR-028) — there's no
   * wizard card for it, so it's deliberately excluded from BILLING_TYPES above. */
  billingType: BillingType | 'Pharmacy';
  departmentId?: string;
  consultantId?: string;
  serviceId?: string;
  /** Set for a Laboratory line that represents a DiagnosticPackage rather than a single
   * DiagnosticService — mutually exclusive with serviceId (never both set on one row). See
   * billingCalculations.ts's laboratoryRowToBillingItem for how a row's itemType/itemId splits
   * into serviceId/packageId. */
  packageId?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  discountApproved: boolean;
  /** Name/username of whoever authorized the discount — set when a non-self-approving role (e.g. Receptionist) had to go through supervisor override. Empty when no override was needed. */
  discountApprovedBy?: string;
  paymentStatus: PaymentStatus;
  total: number;
}

/** One department/consultant pairing on the visit behind a recent bill. */
export interface RecentBillConsultant {
  departmentId: string;
  consultantId: string;
}

/**
 * One row of the Patient Billing page's "Recent Patient Bills" table — the latest N bills
 * across every patient, composed with the demographic/visit context (age, gender, contact,
 * registration type, consultant(s)) the Billing API's `/invoices/recent` endpoint already
 * joins in. Fields beyond invoice/patient identity come back undefined (never an error) when
 * the source patient or visit record can no longer be resolved — see
 * HMS.Modules.Billing.Contracts.RecentPatientBillResponse's own doc comment.
 */
export interface RecentBill {
  invoiceId: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  age?: number;
  gender?: string;
  contactNumber?: string;
  /** OP/IP/Emergency/DayCare/Observation — the visit's own encounter type, i.e. the patient's
   * registration type for that visit. */
  registrationType?: string;
  consultants: RecentBillConsultant[];
  appointmentDateTime: string;
  netAmount: number;
  paymentStatus: PaymentStatus;
  isVoided: boolean;
}

/** The bill for one visit — totals are always derived from `items`, never stored independently. */
export interface Billing {
  id: string;
  /** Human-readable business identifier (e.g. "INV-2026-000001"). Prefer this for display;
   * `id` remains the routing/API key. */
  invoiceNumber?: string;
  patientId: string;
  visitId: string;
  /**
   * Patient name/UHID snapshotted at billing time — not a live join to the patient record.
   * An invoice should keep showing who it was billed to at the time, even if the patient's
   * demographic record is edited later (see docs/DecisionLog.md ADR-008 on billing/edit scope).
   */
  patientName: string;
  patientUhid: string;
  createdAt: string;
  items: BillingItem[];
  grossAmount: number;
  totalDiscount: number;
  netAmount: number;
  isVoided: boolean;
  voidedAt?: string;
  voidReason?: string;
}
