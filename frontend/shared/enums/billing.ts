/** Mirrors HMS.Modules.Billing.Contracts.BillingEnums — serialized as strings (JsonStringEnumConverter).
 * Pharmacy is generated server-side only (DispenseService's best-effort billing step, ADR-028)
 * — there's no manual-entry card for it, so it's deliberately absent from
 * features/billing/types.ts's own BILLING_TYPES (the registration/OPD Billing Entry UI's list
 * of cards staff can add by hand). */
export const BILLING_TYPES = ['Consultation', 'Radiology', 'Laboratory', 'Procedure', 'Pharmacy'] as const;
export type BillingType = (typeof BILLING_TYPES)[number];

export const INVOICE_PAYMENT_STATUSES = ['Pending', 'Paid'] as const;
export type InvoicePaymentStatus = (typeof INVOICE_PAYMENT_STATUSES)[number];

export const PAYMENT_METHODS = ['Cash', 'Card', 'Upi', 'BankTransfer'] as const;
export type PaymentMethod = (typeof PAYMENT_METHODS)[number];
