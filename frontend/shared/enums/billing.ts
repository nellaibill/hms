/** Mirrors HMS.Modules.Billing.Contracts.BillingEnums — serialized as strings (JsonStringEnumConverter). */
export const BILLING_TYPES = ['Consultation', 'Radiology', 'Laboratory', 'Procedure'] as const;
export type BillingType = (typeof BILLING_TYPES)[number];

export const INVOICE_PAYMENT_STATUSES = ['Pending', 'Paid'] as const;
export type InvoicePaymentStatus = (typeof INVOICE_PAYMENT_STATUSES)[number];

export const PAYMENT_METHODS = ['Cash', 'Card', 'Upi', 'BankTransfer'] as const;
export type PaymentMethod = (typeof PAYMENT_METHODS)[number];
