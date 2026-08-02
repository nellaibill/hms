export const BILLING_TYPES = ['Consultation', 'Radiology', 'Laboratory', 'Procedure'] as const;
export type BillingType = (typeof BILLING_TYPES)[number];

export const PAYMENT_STATUSES = ['Pending', 'Paid'] as const;
export type PaymentStatus = (typeof PAYMENT_STATUSES)[number];

/**
 * One priced line on a bill. Normalized so future billing types (packages, insurance, GST
 * line items, refunds…) are just another `billingType` value on the same shape, not a new
 * one — and so a category can later hold many items (multiple lab tests, multiple
 * procedures) without changing this type at all.
 */
export interface BillingItem {
  id: string;
  billingType: BillingType;
  departmentId?: string;
  consultantId?: string;
  serviceId?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  discountApproved: boolean;
  /** Name/username of whoever authorized the discount — set when a non-self-approving role (e.g. Receptionist) had to go through supervisor override. Empty when no override was needed. */
  discountApprovedBy?: string;
  paymentStatus: PaymentStatus;
  total: number;
}

/** The bill for one visit — totals are always derived from `items`, never stored independently. */
export interface Billing {
  id: string;
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
}
