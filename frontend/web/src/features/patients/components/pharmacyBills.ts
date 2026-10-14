import type { SaleResponse } from '@hms/shared';

export type PharmacyBillStatus = 'Paid' | 'Pending' | 'Partially Paid' | 'On IP bill';

export interface PharmacyBillFigures {
  /** What the bill stands at now: its net amount less the value of goods brought back. */
  value: number;
  /** Money kept at the counter: taken less refunded. */
  collected: number;
  /** Still owed at the pharmacy counter. */
  due: number;
  /** The unpaid part put on the patient's admission, settled with the IP final bill. */
  onAdmission: number;
  status: PharmacyBillStatus;
  /** What this bill adds to the patient's Billing totals. The part on the admission is left
   * out: the IP final bill carries it as its Pharmacy line, so counting it here would count
   * it twice. */
  billed: number;
  paid: number;
}

/** A pharmacy bill's money, as the patient's Billing tab shows it. */
export function pharmacyBillFigures(bill: SaleResponse): PharmacyBillFigures {
  const value = bill.netAmount - bill.returnedAmount;
  const collected = bill.paidAmount - bill.refundedAmount;
  const onAdmission = bill.chargedToAdmission ? Math.max(value - collected, 0) : 0;
  const due = bill.chargedToAdmission ? 0 : Math.max(bill.balanceDue, 0);
  const status: PharmacyBillStatus = bill.chargedToAdmission ? 'On IP bill' : due <= 0 ? 'Paid' : collected > 0 ? 'Partially Paid' : 'Pending';
  return { value, collected, due, onAdmission, status, billed: value - onAdmission, paid: collected };
}
