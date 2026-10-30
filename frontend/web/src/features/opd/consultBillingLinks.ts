/**
 * OPD "Consult" is only allowed once the visit's Payment Status is Paid. An unpaid visit's
 * Consult click sends the user to billing with these query params, so the billing page knows
 * which consultation to continue to once it's paid (OpdConsultAfterBillingBanner).
 */
export const CONSULT_AFTER_BILLING_PARAM = 'consultationId';

interface ConsultBillingTarget {
  patientId: string;
  visitId: string;
  consultationId: string;
}

/** OPD Billing Entry for a visit that has no invoice yet — bills that visit, not just the
 * patient's latest one. */
export function billingEntryPathForConsult({ patientId, visitId, consultationId }: ConsultBillingTarget): string {
  const params = new URLSearchParams({ patientId, visitId, [CONSULT_AFTER_BILLING_PARAM]: consultationId });
  return `/finance/accounts/new?${params.toString()}`;
}

/** Invoice Detail for a visit's unpaid invoice — Record Payment is collected there. */
export function invoicePathForConsult(invoiceId: string, consultationId: string): string {
  const params = new URLSearchParams({ [CONSULT_AFTER_BILLING_PARAM]: consultationId });
  return `/finance/accounts/${invoiceId}?${params.toString()}`;
}
